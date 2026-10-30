using Microsoft.EntityFrameworkCore;
using ProductCatalog.Api.Data;
using ProductCatalog.Api.Models;

namespace ProductCatalog.Api.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly AppDbContext _dbContext;

    public ProductRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<Product>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Products
            .AsNoTracking()
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Product>> SearchAsync(
        string? name,
        decimal? minPrice,
        decimal? maxPrice,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Products.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(name))
        {
            var term = name.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(term) || p.Description.ToLower().Contains(term));
        }

        if (minPrice.HasValue)
        {
            query = query.Where(p => p.Price >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(p => p.Price <= maxPrice.Value);
        }

        return await query
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Product> CreateAsync(Product product, CancellationToken cancellationToken = default)
    {
        product.CreatedAt = DateTime.UtcNow;
        await _dbContext.Products.AddAsync(product, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return product;
    }

    public async Task<Product?> UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == product.Id, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        existing.Name = product.Name;
        existing.Description = product.Description;
        existing.Price = product.Price;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (existing is null)
        {
            return false;
        }

        _dbContext.Products.Remove(existing);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
