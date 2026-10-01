namespace SalesForecastSystem.Core.DTOs.Customers;

public sealed record CustomerResponse(int CustomerId, string FullName, string? Email,
    string? PhoneNumber, string? Address, DateTime CreatedAt, bool IsActive);

public sealed record CustomerOrderResponse(long SalesOrderId, string OrderNumber,
    DateTime OrderDate, string Status, DateTime CreatedAt);
