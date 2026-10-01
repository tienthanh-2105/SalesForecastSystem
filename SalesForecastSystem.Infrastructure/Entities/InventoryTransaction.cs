namespace SalesForecastSystem.Infrastructure.Entities;

public sealed class InventoryTransaction
{
    public long InventoryTransactionId { get; set; }
    public int WarehouseId { get; set; }
    public int ProductId { get; set; }
    public long? PurchaseOrderItemId { get; set; }
    public long? SalesOrderItemId { get; set; }
    public int Quantity { get; set; }
    public DateTime TransactionDate { get; set; }
    public DateTime CreatedAt { get; set; }
}
