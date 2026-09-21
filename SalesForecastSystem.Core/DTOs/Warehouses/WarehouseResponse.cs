namespace SalesForecastSystem.Core.DTOs.Warehouses;

public sealed record WarehouseResponse(int WarehouseId, string Name, string? Address, bool IsActive);
