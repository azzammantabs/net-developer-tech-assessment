using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ProductCatalog.Api.DTOs;
using ProductCatalog.Api.Models;
using ProductCatalog.Api.Repositories;
using ProductCatalog.Api.Security;
using ProductCatalog.Api.Services;

namespace ProductCatalog.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("CriticalEndpoints")]
public class AuthController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IUserRepository userRepository,
        IJwtTokenService jwtTokenService,
        ILogger<AuthController> logger)
    {
        _userRepository = userRepository;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (InputSanitizer.ContainsSqlOrScriptInjection(request.Username) ||
            InputSanitizer.ContainsSqlOrScriptInjection(request.Email))
        {
            return BadRequest(new { message = "Input contains potentially unsafe characters." });
        }

        var sanitizedUsername = InputSanitizer.Sanitize(request.Username);
        var sanitizedEmail = InputSanitizer.Sanitize(request.Email);

        if (await _userRepository.ExistsByUsernameOrEmailAsync(sanitizedUsername, sanitizedEmail, cancellationToken))
        {
            return Conflict(new { message = "Username or email is already registered." });
        }

        var newUser = new AppUser
        {
            Username = sanitizedUsername,
            Email = sanitizedEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = "Admin"
        };

        var createdUser = await _userRepository.CreateAsync(newUser, cancellationToken);
        _logger.LogInformation("New user registered: {Username}", createdUser.Username);

        var authResponse = _jwtTokenService.GenerateToken(createdUser);
        return CreatedAtAction(nameof(Register), new { username = createdUser.Username }, authResponse);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var user = await _userRepository.GetByUsernameAsync(request.Username, cancellationToken);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Failed login attempt for username: {Username}", request.Username);
            return Unauthorized(new { message = "Invalid username or password." });
        }

        var authResponse = _jwtTokenService.GenerateToken(user);
        _logger.LogInformation("User logged in successfully: {Username}", user.Username);
        return Ok(authResponse);
    }
}
