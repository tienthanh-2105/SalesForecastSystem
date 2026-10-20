using System.ComponentModel.DataAnnotations;

namespace SalesForecastSystem.Core.DTOs.Forecasting;

public sealed class ForecastRunRequest : IValidatableObject
{
    [Required, Range(1, int.MaxValue)] public int? ProductId { get; set; }
    [Required, Range(1, int.MaxValue)] public int? WarehouseId { get; set; }
    [Required] public DateTime? FromDate { get; set; }
    [Required] public DateTime? ToDate { get; set; }
    [Range(1, 366)] public int ForecastDays { get; set; } = 7;
    [Range(10, 50)] public int ValidationPercentage { get; set; } = 20;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (FromDate.HasValue && ToDate.HasValue && ToDate.Value.Date <= FromDate.Value.Date)
            yield return new ValidationResult("To date must be after from date.", [nameof(ToDate)]);
        if (FromDate.HasValue && ToDate.HasValue && (ToDate.Value.Date - FromDate.Value.Date).TotalDays > 1094)
            yield return new ValidationResult("Forecast data range cannot exceed 1095 days.", [nameof(ToDate)]);
    }
}
