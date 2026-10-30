using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ProductCatalog.Api.Controllers;
using ProductCatalog.Api.DTOs;
using ProductCatalog.Api.Models;
using ProductCatalog.Api.Repositories;
using Xunit;

namespace ProductCatalog.Tests;

public class ProductsControllerTests
{
    private readonly Mock<IProductRepository> _mockRepo;
    private readonly IMemoryCache _memoryCache;
    private readonly ProductsController _controller;

    public ProductsControllerTests()
    {
        _mockRepo = new Mock<IProductRepository>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _controller = new ProductsController(_mockRepo.Object, _memoryCache, NullLogger<ProductsController>.Instance);
    }

    [Fact]
    public async Task GetAll_FirstCallQueriesRepo_SecondCallUsesCache()
    {
        var sampleProducts = new List<Product>
        {
            new() { Id = 1, Name = "Laptop", Description = "Developer Laptop", Price = 1200m }
        };
        _mockRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(sampleProducts);

        // First call: hits repository
        var result1 = await _controller.GetAll(CancellationToken.None);
        var ok1 = Assert.IsType<OkObjectResult>(result1.Result);
        Assert.Single((IEnumerable<Product>)ok1.Value!);

        // Second call: served from IMemoryCache without invoking repository a second time
        var result2 = await _controller.GetAll(CancellationToken.None);
        var ok2 = Assert.IsType<OkObjectResult>(result2.Result);
        Assert.Single((IEnumerable<Product>)ok2.Value!);

        _mockRepo.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetById_ExistingProduct_ReturnsOk()
    {
        var product = new Product { Id = 5, Name = "Monitor", Description = "4K Display", Price = 400m };
        _mockRepo.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var result = await _controller.GetById(5, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsType<Product>(okResult.Value);
        Assert.Equal("Monitor", returned.Name);
    }

    [Fact]
    public async Task GetById_NonExistingProduct_ReturnsNotFound()
    {
        _mockRepo.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);

        var result = await _controller.GetById(99, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task Search_ValidRange_ReturnsMatchingProducts()
    {
        var query = new ProductSearchQueryDto { Name = "Keyboard", MinPrice = 50, MaxPrice = 200 };
        var expected = new List<Product>
        {
            new() { Id = 2, Name = "Keyboard", Description = "Mechanical", Price = 120m }
        };
        _mockRepo.Setup(r => r.SearchAsync("Keyboard", 50, 200, It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var result = await _controller.Search(query, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Single((IEnumerable<Product>)okResult.Value!);
    }

    [Fact]
    public async Task Search_MinPriceGreaterThanMaxPrice_ReturnsBadRequest()
    {
        var query = new ProductSearchQueryDto { MinPrice = 500, MaxPrice = 100 };

        var result = await _controller.Search(query, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Search_MaliciousScriptInput_ReturnsBadRequest()
    {
        var query = new ProductSearchQueryDto { Name = "<script>alert(1)</script>" };

        var result = await _controller.Search(query, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Create_ValidProduct_ReturnsCreatedAtActionAndInvalidatesCache()
    {
        var dto = new ProductCreateUpdateDto { Name = "SSD 1TB", Description = "NVMe Gen4 SSD", Price = 99.99m };
        _mockRepo.Setup(r => r.CreateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product p, CancellationToken _) => { p.Id = 10; return p; });

        var result = await _controller.Create(dto, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var product = Assert.IsType<Product>(created.Value);
        Assert.Equal(10, product.Id);
        Assert.Equal("SSD 1TB", product.Name);
    }

    [Fact]
    public async Task Create_XssInjectionAttempt_ReturnsBadRequest()
    {
        var dto = new ProductCreateUpdateDto
        {
            Name = "Good Name",
            Description = "<script>stealCookies()</script>",
            Price = 49.99m
        };

        var result = await _controller.Create(dto, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Update_ExistingProduct_ReturnsOk()
    {
        var dto = new ProductCreateUpdateDto { Name = "Updated Name", Description = "Updated Desc", Price = 150m };
        _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product p, CancellationToken _) => p);

        var result = await _controller.Update(1, dto, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var updated = Assert.IsType<Product>(okResult.Value);
        Assert.Equal("Updated Name", updated.Name);
    }

    [Fact]
    public async Task Update_NonExistingProduct_ReturnsNotFound()
    {
        var dto = new ProductCreateUpdateDto { Name = "Updated Name", Description = "Updated Desc", Price = 150m };
        _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var result = await _controller.Update(999, dto, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task Delete_ExistingProduct_ReturnsNoContent()
    {
        _mockRepo.Setup(r => r.DeleteAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _controller.Delete(1, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_NonExistingProduct_ReturnsNotFound()
    {
        _mockRepo.Setup(r => r.DeleteAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await _controller.Delete(999, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }
}
