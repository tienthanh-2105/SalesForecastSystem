namespace SalesForecastSystem.Infrastructure.Entities;

public sealed class SalesOrder
{
    public long SalesOrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public int WarehouseId { get; set; }
    public int? CustomerId { get; set; }
    public int CreatedByUserId { get; set; }
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = "Draft";
    public string? Notes { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string? ShippingAddress { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public ICollection<SalesOrderItem> Items { get; set; } = [];
}

public sealed class SalesOrderItem
{
    public long SalesOrderItemId { get; set; }
    public long SalesOrderId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
    public decimal LineTotal { get; private set; }
}
