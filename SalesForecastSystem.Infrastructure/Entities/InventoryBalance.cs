namespace SalesForecastSystem.Infrastructure.Entities;

public sealed class InventoryBalance
{
    public int WarehouseId { get; set; }
    public int ProductId { get; set; }
    public long QuantityOnHand { get; set; }
}
