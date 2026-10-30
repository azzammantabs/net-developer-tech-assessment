using Microsoft.EntityFrameworkCore;
using ProductCatalog.Api.Data;
using ProductCatalog.Api.Models;
using ProductCatalog.Api.Repositories;
using Xunit;

namespace ProductCatalog.Tests;

public class ProductRepositoryTests
{
    private static AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task CrudAndSearchOperations_WorkAsExpected()
    {
        await using var context = CreateInMemoryContext();
        var repo = new ProductRepository(context);

        var created = await repo.CreateAsync(new Product
        {
            Name = "Mechanical Keyboard",
            Description = "RGB Backlit",
            Price = 120m
        });

        Assert.True(created.Id > 0);

        var byId = await repo.GetByIdAsync(created.Id);
        Assert.NotNull(byId);

        var searchResults = await repo.SearchAsync("Keyboard", 100m, 150m);
        Assert.Single(searchResults);

        created.Price = 99m;
        var updated = await repo.UpdateAsync(created);
        Assert.Equal(99m, updated!.Price);

        var deleted = await repo.DeleteAsync(created.Id);
        Assert.True(deleted);
    }
}
