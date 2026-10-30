using System.ComponentModel.DataAnnotations;

namespace ProductCatalog.Api.DTOs;

public class ProductCreateUpdateDto
{
    [Required(ErrorMessage = "Product name is required.")]
    [StringLength(120, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 120 characters.")]
    [RegularExpression(@"^[a-zA-Z0-9\s\-_.,()&/]+$", ErrorMessage = "Name contains invalid or potentially unsafe characters.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Product description is required.")]
    [StringLength(1000, MinimumLength = 5, ErrorMessage = "Description must be between 5 and 1000 characters.")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Price is required.")]
    [Range(typeof(decimal), "0.01", "999999999.99", ErrorMessage = "Price must be between 0.01 and 999,999,999.99.")]
    public decimal Price { get; set; }
}

public class ProductSearchQueryDto
{
    [StringLength(100, ErrorMessage = "Search keyword cannot exceed 100 characters.")]
    public string? Name { get; set; }

    [Range(typeof(decimal), "0", "999999999.99", ErrorMessage = "Minimum price must be non-negative.")]
    public decimal? MinPrice { get; set; }

    [Range(typeof(decimal), "0", "999999999.99", ErrorMessage = "Maximum price must be non-negative.")]
    public decimal? MaxPrice { get; set; }
}
