namespace SalesForecastSystem.Core.DTOs.Products;

public sealed record ProductListItemResponse(
    int ProductId,
    int CategoryId,
    string CategoryName,
    string SKU,
    string Name,
    string Unit,
    decimal SalePrice,
    int MinimumStockLevel,
    long QuantityOnHand,
    string StockStatus,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);
