namespace SalesForecastSystem.Core.DTOs.Purchases;

public sealed record PurchaseItemResponse(long PurchaseOrderItemId, int ProductId, int Quantity,
    decimal UnitPrice, decimal LineTotal);

public sealed record PurchaseResponse(long PurchaseOrderId, string OrderNumber, int WarehouseId,
    int SupplierId, int CreatedByUserId, DateTime OrderDate, string Status, string? Notes,
    DateTime CreatedAt, DateTime? PostedAt, decimal TotalAmount, IReadOnlyList<PurchaseItemResponse> Items);

public sealed record PurchaseListItemResponse(long PurchaseOrderId, string OrderNumber, int WarehouseId,
    int SupplierId, DateTime OrderDate, string Status, DateTime CreatedAt);
