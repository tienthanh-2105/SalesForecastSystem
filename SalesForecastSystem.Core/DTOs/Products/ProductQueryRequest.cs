using System.ComponentModel.DataAnnotations;

namespace SalesForecastSystem.Core.DTOs.Products;

public enum ProductStockStatus
{
    OutOfStock,
    Low,
    Available
}

public enum ProductSortBy
{
    Name,
    SKU,
    SalePrice,
    CreatedAt,
    QuantityOnHand
}

public enum SortDirection
{
    Asc,
    Desc
}

public sealed class ProductQueryRequest : IValidatableObject
{
    [StringLength(200, ErrorMessage = "Search cannot exceed 200 characters.")]
    public string? Search { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Category ID must be a positive integer.")]
    public int? CategoryId { get; set; }

    public bool? IsActive { get; set; }
    public ProductStockStatus? StockStatus { get; set; }
    public ProductSortBy SortBy { get; set; } = ProductSortBy.Name;
    public SortDirection SortDirection { get; set; } = SortDirection.Asc;

    [Range(1, int.MaxValue, ErrorMessage = "Page must be at least 1.")]
    public int Page { get; set; } = 1;

    [Range(1, 100, ErrorMessage = "Page size must be between 1 and 100.")]
    public int PageSize { get; set; } = 20;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StockStatus.HasValue && !Enum.IsDefined(StockStatus.Value))
        {
            yield return new ValidationResult("Stock status is invalid.", [nameof(StockStatus)]);
        }

        if (!Enum.IsDefined(SortBy))
        {
            yield return new ValidationResult("Sort field is invalid.", [nameof(SortBy)]);
        }

        if (!Enum.IsDefined(SortDirection))
        {
            yield return new ValidationResult("Sort direction is invalid.", [nameof(SortDirection)]);
        }
    }
}
