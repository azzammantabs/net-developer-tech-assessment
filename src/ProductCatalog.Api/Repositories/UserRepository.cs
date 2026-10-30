using Microsoft.EntityFrameworkCore;
using ProductCatalog.Api.Data;
using ProductCatalog.Api.Models;

namespace ProductCatalog.Api.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _dbContext;

    public UserRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AppUser?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        var normalized = username.Trim().ToLower();
        return await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username.ToLower() == normalized, cancellationToken);
    }

    public async Task<bool> ExistsByUsernameOrEmailAsync(string username, string email, CancellationToken cancellationToken = default)
    {
        var normUser = username.Trim().ToLower();
        var normEmail = email.Trim().ToLower();
        return await _dbContext.Users
            .AnyAsync(u => u.Username.ToLower() == normUser || u.Email.ToLower() == normEmail, cancellationToken);
    }

    public async Task<AppUser> CreateAsync(AppUser user, CancellationToken cancellationToken = default)
    {
        user.CreatedAt = DateTime.UtcNow;
        await _dbContext.Users.AddAsync(user, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return user;
    }
}
