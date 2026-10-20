namespace SalesForecastSystem.Infrastructure.Entities;

using SalesForecastSystem.Core.Enums;

public sealed class PurchaseOrder
{
    public long PurchaseOrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public int WarehouseId { get; set; }
    public int SupplierId { get; set; }
    public int CreatedByUserId { get; set; }
    public DateTime OrderDate { get; set; }
    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PostedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public ICollection<PurchaseOrderItem> Items { get; set; } = [];
}

public sealed class PurchaseOrderItem
{
    public long PurchaseOrderItemId { get; set; }
    public long PurchaseOrderId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; private set; }
}
