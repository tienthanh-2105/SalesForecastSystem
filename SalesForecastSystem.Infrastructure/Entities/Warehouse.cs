namespace SalesForecastSystem.Infrastructure.Entities;

public sealed class Warehouse
{
    public int WarehouseId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;
}
