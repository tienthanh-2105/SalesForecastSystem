namespace SalesForecastSystem.Core.DTOs.Products;

public sealed record ProductStockResponse(
    int ProductId,
    long QuantityOnHand,
    long ReservedQuantity = 0,
    long AvailableQuantity = 0);
