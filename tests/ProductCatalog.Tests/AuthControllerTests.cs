using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ProductCatalog.Api.Controllers;
using ProductCatalog.Api.DTOs;
using ProductCatalog.Api.Models;
using ProductCatalog.Api.Repositories;
using ProductCatalog.Api.Services;
using Xunit;

namespace ProductCatalog.Tests;

public class AuthControllerTests
{
    private readonly Mock<IUserRepository> _mockUserRepo;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _mockUserRepo = new Mock<IUserRepository>();
        var inMemorySettings = new Dictionary<string, string?>
        {
            {"JwtSettings:SecretKey", "TestSecretKeyWithMoreThan32Characters_12345!"},
            {"JwtSettings:Issuer", "TestIssuer"},
            {"JwtSettings:Audience", "TestAudience"},
            {"JwtSettings:ExpiryMinutes", "60"}
        };
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        _jwtTokenService = new JwtTokenService(configuration);
        _controller = new AuthController(_mockUserRepo.Object, _jwtTokenService, NullLogger<AuthController>.Instance);
    }

    [Fact]
    public async Task Register_NewValidUser_ReturnsCreatedWithJwtToken()
    {
        var dto = new RegisterRequestDto
        {
            Username = "newdev",
            Email = "newdev@example.com",
            Password = "Password123!"
        };
        _mockUserRepo.Setup(r => r.ExistsByUsernameOrEmailAsync("newdev", "newdev@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockUserRepo.Setup(r => r.CreateAsync(It.IsAny<AppUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppUser u, CancellationToken _) => { u.Id = 1; return u; });

        var result = await _controller.Register(dto, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        var response = Assert.IsType<AuthResponseDto>(created.Value);
        Assert.False(string.IsNullOrWhiteSpace(response.Token));
        Assert.Equal("newdev", response.Username);
    }

    [Fact]
    public async Task Register_DuplicateUser_ReturnsConflict()
    {
        var dto = new RegisterRequestDto
        {
            Username = "existing",
            Email = "existing@example.com",
            Password = "Password123!"
        };
        _mockUserRepo.Setup(r => r.ExistsByUsernameOrEmailAsync("existing", "existing@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _controller.Register(dto, CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOkWithJwtToken()
    {
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword("SecretPass123");
        var existingUser = new AppUser
        {
            Id = 1,
            Username = "admin",
            Email = "admin@example.com",
            PasswordHash = hashedPassword
        };
        _mockUserRepo.Setup(r => r.GetByUsernameAsync("admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        var result = await _controller.Login(new LoginRequestDto { Username = "admin", Password = "SecretPass123" }, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var auth = Assert.IsType<AuthResponseDto>(okResult.Value);
        Assert.False(string.IsNullOrWhiteSpace(auth.Token));
    }

    [Fact]
    public async Task Login_InvalidPassword_ReturnsUnauthorized()
    {
        var existingUser = new AppUser
        {
            Id = 1,
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword")
        };
        _mockUserRepo.Setup(r => r.GetByUsernameAsync("admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        var result = await _controller.Login(new LoginRequestDto { Username = "admin", Password = "WrongPassword" }, CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }
}
