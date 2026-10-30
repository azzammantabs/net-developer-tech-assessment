using ProductCatalog.Api.Models;

namespace ProductCatalog.Api.Repositories;

public interface IUserRepository
{
    Task<AppUser?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<bool> ExistsByUsernameOrEmailAsync(string username, string email, CancellationToken cancellationToken = default);
    Task<AppUser> CreateAsync(AppUser user, CancellationToken cancellationToken = default);
}
