using System.ComponentModel.DataAnnotations;

namespace SalesForecastSystem.Core.DTOs.Reports;

public enum SalesReportPeriod
{
    Day,
    Week,
    Month
}

public class SalesReportQuery : IValidatableObject
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }

    [Range(1, int.MaxValue)]
    public int? WarehouseId { get; set; }

    [Range(1, int.MaxValue)]
    public int? ProductId { get; set; }

    [Range(1, int.MaxValue)]
    public int? CategoryId { get; set; }

    [Range(1, int.MaxValue)]
    public int? StaffUserId { get; set; }

    public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (FromDate.HasValue && ToDate.HasValue && FromDate.Value.Date > ToDate.Value.Date)
        {
            yield return new ValidationResult(
                "From date cannot be later than to date.",
                [nameof(FromDate), nameof(ToDate)]);
        }

        if (FromDate.HasValue && ToDate.HasValue &&
            (ToDate.Value.Date - FromDate.Value.Date).TotalDays > 366)
        {
            yield return new ValidationResult(
                "Report date range cannot exceed 366 days.",
                [nameof(FromDate), nameof(ToDate)]);
        }
    }
}

public sealed class SalesTrendQuery : SalesReportQuery
{
    public SalesReportPeriod Period { get; set; } = SalesReportPeriod.Day;

    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var result in base.Validate(validationContext))
            yield return result;

        if (!Enum.IsDefined(Period))
        {
            yield return new ValidationResult(
                "Period must be Day, Week, or Month.",
                [nameof(Period)]);
        }
    }
}

public sealed class SalesRankingQuery : SalesReportQuery
{
    [Range(1, 100)]
    public int Top { get; set; } = 10;
}

public sealed class InventoryReportQuery
{
    [Range(1, int.MaxValue)]
    public int? WarehouseId { get; set; }

    public bool BelowMinimumOnly { get; set; }
}
