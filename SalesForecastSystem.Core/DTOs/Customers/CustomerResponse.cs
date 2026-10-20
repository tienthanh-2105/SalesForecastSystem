namespace SalesForecastSystem.Core.DTOs.Customers;

using SalesForecastSystem.Core.Enums;

public sealed record CustomerResponse(int CustomerId, string FullName, string? Email,
    string? PhoneNumber, string? Address, DateTime CreatedAt, bool IsActive);

public sealed record CustomerOrderResponse(long SalesOrderId, string OrderNumber,
    DateTime OrderDate, SalesOrderStatus Status, DateTime CreatedAt);
