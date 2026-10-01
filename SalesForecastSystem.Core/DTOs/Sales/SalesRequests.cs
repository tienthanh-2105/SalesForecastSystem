using System.ComponentModel.DataAnnotations;

namespace SalesForecastSystem.Core.DTOs.Sales;

public sealed class SalesQueryRequest : IValidatableObject
{
    [StringLength(200)] public string? Search { get; set; }
    [Range(1, int.MaxValue)] public int Page { get; set; } = 1;
    [Range(1, 100)] public int PageSize { get; set; } = 20;
    [Range(1, int.MaxValue)] public int? WarehouseId { get; set; }
    [Range(1, int.MaxValue)] public int? CustomerId { get; set; }
    [Range(1, int.MaxValue)] public int? CreatedByUserId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    [RegularExpression("Draft|Pending|Delivering|Completed|Cancelled")] public string? Status { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (FromDate.HasValue && ToDate.HasValue && FromDate > ToDate)
            yield return new ValidationResult("FromDate cannot be later than ToDate.", [nameof(FromDate), nameof(ToDate)]);
    }
}

public sealed class SalesRequest : IValidatableObject
{
    [Required, StringLength(50)] public string OrderNumber { get; set; } = string.Empty;
    [Range(1, int.MaxValue)] public int WarehouseId { get; set; }
    [Range(1, int.MaxValue)] public int? CustomerId { get; set; }
    [Required] public DateTime OrderDate { get; set; }
    [StringLength(500)] public string? Notes { get; set; }
    [StringLength(255)] public string? ShippingAddress { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (string.IsNullOrWhiteSpace(OrderNumber))
            yield return new ValidationResult("Order number is required.", [nameof(OrderNumber)]);
        if (OrderDate == default)
            yield return new ValidationResult("Order date is required.", [nameof(OrderDate)]);
    }
}

public sealed class SalesItemRequest : IValidatableObject
{
    [Range(1, int.MaxValue)] public int ProductId { get; set; }
    [Range(1, int.MaxValue)] public int Quantity { get; set; }
    [Range(typeof(decimal), "0", "9999999999999999.99")] public decimal UnitPrice { get; set; }
    [Range(typeof(decimal), "0", "9999999999999999.99")] public decimal Discount { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (Discount > Quantity * UnitPrice)
            yield return new ValidationResult("Discount cannot exceed line amount.", [nameof(Discount)]);
    }
}
