namespace SalesForecastSystem.Core.DTOs.Products;

public sealed record ProductResponse(
    int ProductId,
    int CategoryId,
    string SKU,
    string Name,
    string Unit,
    string? Description,
    string? ImageUrl,
    decimal SalePrice,
    int MinimumStockLevel,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string RowVersion);
