using System.ComponentModel.DataAnnotations;

namespace SalesForecastSystem.Core.DTOs.Inventory;

public sealed class InventoryTransactionQuery : IValidatableObject
{
    [Range(1, int.MaxValue)] public int Page { get; set; } = 1;
    [Range(1, 100)] public int PageSize { get; set; } = 20;
    [Range(1, int.MaxValue)] public int? WarehouseId { get; set; }
    [Range(1, int.MaxValue)] public int? ProductId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (FromDate.HasValue && ToDate.HasValue && FromDate > ToDate)
            yield return new ValidationResult("FromDate cannot be later than ToDate.", [nameof(FromDate), nameof(ToDate)]);
    }
}

public sealed record InventoryTransactionResponse(long InventoryTransactionId, int WarehouseId,
    int ProductId, long? PurchaseOrderItemId, long? SalesOrderItemId, int Quantity,
    DateTime TransactionDate, DateTime CreatedAt);
