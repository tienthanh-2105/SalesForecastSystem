using System.ComponentModel.DataAnnotations;

namespace SalesForecastSystem.Core.DTOs.Purchases;

public sealed class PurchaseQueryRequest : IValidatableObject
{
    [Range(1, int.MaxValue)] public int Page { get; set; } = 1;
    [Range(1, 100)] public int PageSize { get; set; } = 20;
    [Range(1, int.MaxValue)] public int? WarehouseId { get; set; }
    [Range(1, int.MaxValue)] public int? SupplierId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    [RegularExpression("Draft|Posted|Cancelled")] public string? Status { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (FromDate.HasValue && ToDate.HasValue && FromDate > ToDate)
            yield return new ValidationResult("FromDate cannot be later than ToDate.", [nameof(FromDate), nameof(ToDate)]);
    }
}

public sealed class PurchaseRequest : IValidatableObject
{
    [Required, StringLength(50)] public string OrderNumber { get; set; } = string.Empty;
    [Range(1, int.MaxValue)] public int WarehouseId { get; set; }
    [Range(1, int.MaxValue)] public int SupplierId { get; set; }
    [Required] public DateTime OrderDate { get; set; }
    [StringLength(500)] public string? Notes { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (string.IsNullOrWhiteSpace(OrderNumber))
            yield return new ValidationResult("Order number is required.", [nameof(OrderNumber)]);
        if (OrderDate == default)
            yield return new ValidationResult("Order date is required.", [nameof(OrderDate)]);
    }
}

public sealed class PurchaseItemRequest
{
    [Range(1, int.MaxValue)] public int ProductId { get; set; }
    [Range(1, int.MaxValue)] public int Quantity { get; set; }
    [Range(typeof(decimal), "0", "9999999999999999.99")] public decimal UnitPrice { get; set; }
}
