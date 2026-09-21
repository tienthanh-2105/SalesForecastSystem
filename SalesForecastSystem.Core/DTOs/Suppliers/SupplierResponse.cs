namespace SalesForecastSystem.Core.DTOs.Suppliers;

public sealed record SupplierResponse(
    int SupplierId, string Name, string? TaxCode, string? Email,
    string? PhoneNumber, string? Address, bool IsActive);
