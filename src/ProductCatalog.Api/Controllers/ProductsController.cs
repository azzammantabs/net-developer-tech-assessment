using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Memory;
using ProductCatalog.Api.DTOs;
using ProductCatalog.Api.Models;
using ProductCatalog.Api.Repositories;
using ProductCatalog.Api.Security;

namespace ProductCatalog.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private const string ProductListCacheKey = "products_all_cache";
    private readonly IProductRepository _productRepository;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(
        IProductRepository productRepository,
        IMemoryCache memoryCache,
        ILogger<ProductsController> logger)
    {
        _productRepository = productRepository;
        _memoryCache = memoryCache;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves all products with In-Memory Caching for fast repeated reads.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<Product>>> GetAll(CancellationToken cancellationToken)
    {
        if (_memoryCache.TryGetValue(ProductListCacheKey, out IEnumerable<Product>? cachedProducts) && cachedProducts is not null)
        {
            _logger.LogInformation("Returning product list from in-memory cache.");
            return Ok(cachedProducts);
        }

        var products = await _productRepository.GetAllAsync(cancellationToken);
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(5))
            .SetSlidingExpiration(TimeSpan.FromMinutes(2));

        _memoryCache.Set(ProductListCacheKey, products, cacheOptions);
        return Ok(products);
    }

    /// <summary>
    /// Retrieves a specific product by its ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<Product>> GetById(int id, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(id, cancellationToken);
        if (product is null)
        {
            return NotFound(new { message = $"Product with ID {id} was not found." });
        }

        return Ok(product);
    }

    /// <summary>
    /// Searches and filters products by name keyword and/or price range (minPrice, maxPrice).
    /// </summary>
    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<Product>>> Search(
        [FromQuery] ProductSearchQueryDto query,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (InputSanitizer.ContainsSqlOrScriptInjection(query.Name))
        {
            return BadRequest(new { message = "Search query contains unsafe or prohibited patterns." });
        }

        if (query.MinPrice.HasValue && query.MaxPrice.HasValue && query.MinPrice > query.MaxPrice)
        {
            return BadRequest(new { message = "MinPrice cannot be greater than MaxPrice." });
        }

        var sanitizedName = string.IsNullOrWhiteSpace(query.Name) ? null : InputSanitizer.Sanitize(query.Name);
        var results = await _productRepository.SearchAsync(sanitizedName, query.MinPrice, query.MaxPrice, cancellationToken);
        return Ok(results);
    }

    /// <summary>
    /// Creates a new product (Protected by JWT Authorization and Rate Limiting).
    /// </summary>
    [HttpPost]
    [Authorize]
    [EnableRateLimiting("CriticalEndpoints")]
    public async Task<ActionResult<Product>> Create(
        [FromBody] ProductCreateUpdateDto dto,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (InputSanitizer.ContainsSqlOrScriptInjection(dto.Name) ||
            InputSanitizer.ContainsSqlOrScriptInjection(dto.Description))
        {
            return BadRequest(new { message = "Product input contains unsafe or prohibited script/injection patterns." });
        }

        var product = new Product
        {
            Name = InputSanitizer.Sanitize(dto.Name),
            Description = InputSanitizer.Sanitize(dto.Description),
            Price = dto.Price,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _productRepository.CreateAsync(product, cancellationToken);
        _memoryCache.Remove(ProductListCacheKey);
        _logger.LogInformation("Product created with ID {ProductId}: {ProductName}", created.Id, created.Name);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>
    /// Updates an existing product by ID (Protected by JWT Authorization and Rate Limiting).
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize]
    [EnableRateLimiting("CriticalEndpoints")]
    public async Task<ActionResult<Product>> Update(
        int id,
        [FromBody] ProductCreateUpdateDto dto,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (InputSanitizer.ContainsSqlOrScriptInjection(dto.Name) ||
            InputSanitizer.ContainsSqlOrScriptInjection(dto.Description))
        {
            return BadRequest(new { message = "Product input contains unsafe or prohibited script/injection patterns." });
        }

        var productToUpdate = new Product
        {
            Id = id,
            Name = InputSanitizer.Sanitize(dto.Name),
            Description = InputSanitizer.Sanitize(dto.Description),
            Price = dto.Price
        };

        var updated = await _productRepository.UpdateAsync(productToUpdate, cancellationToken);
        if (updated is null)
        {
            return NotFound(new { message = $"Product with ID {id} was not found." });
        }

        _memoryCache.Remove(ProductListCacheKey);
        _logger.LogInformation("Product ID {ProductId} updated.", id);
        return Ok(updated);
    }

    /// <summary>
    /// Deletes an existing product by ID (Protected by JWT Authorization and Rate Limiting).
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize]
    [EnableRateLimiting("CriticalEndpoints")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await _productRepository.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound(new { message = $"Product with ID {id} was not found." });
        }

        _memoryCache.Remove(ProductListCacheKey);
        _logger.LogInformation("Product ID {ProductId} deleted.", id);
        return NoContent();
    }
}
