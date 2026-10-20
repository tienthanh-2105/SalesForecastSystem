namespace SalesForecastSystem.Core.DTOs.Sales;

using SalesForecastSystem.Core.Enums;

public sealed record SalesItemResponse(long SalesOrderItemId, int ProductId, int Quantity,
    decimal UnitPrice, decimal Discount, decimal LineTotal);

public sealed record SalesResponse(long SalesOrderId, string OrderNumber, int WarehouseId,
    int? CustomerId, int CreatedByUserId, DateTime OrderDate, SalesOrderStatus Status, string? Notes,
    string? CustomerName, string? CustomerPhone, string? ShippingAddress,
    DateTime CreatedAt, DateTime? CompletedAt, decimal TotalAmount, IReadOnlyList<SalesItemResponse> Items);

public sealed record SalesListItemResponse(long SalesOrderId, string OrderNumber, int WarehouseId,
    int? CustomerId, int CreatedByUserId, DateTime OrderDate, SalesOrderStatus Status, DateTime CreatedAt,
    string? CustomerName, string? CustomerPhone, string? ShippingAddress, decimal TotalAmount);
