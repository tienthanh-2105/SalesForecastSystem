using System.ComponentModel.DataAnnotations;

namespace SalesForecastSystem.Core.DTOs.Forecasting;

public sealed class ForecastDataQuery : IValidatableObject
{
    [Required(ErrorMessage = "Product ID is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Product ID must be a positive integer.")]
    public int? ProductId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Warehouse ID must be a positive integer.")]
    public int? WarehouseId { get; set; }

    [Required(ErrorMessage = "From date is required.")]
    public DateTime? FromDate { get; set; }

    [Required(ErrorMessage = "To date is required.")]
    public DateTime? ToDate { get; set; }

    [Range(10, 50, ErrorMessage = "Validation percentage must be between 10 and 50.")]
    public int ValidationPercentage { get; set; } = 20;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!FromDate.HasValue || !ToDate.HasValue)
            yield break;

        var days = (ToDate.Value.Date - FromDate.Value.Date).TotalDays;
        if (days < 0)
        {
            yield return new ValidationResult(
                "From date cannot be later than to date.",
                [nameof(FromDate), nameof(ToDate)]);
        }
        else if (days < 1)
        {
            yield return new ValidationResult(
                "Forecast dataset must contain at least two days.",
                [nameof(FromDate), nameof(ToDate)]);
        }
        else if (days > 1094)
        {
            yield return new ValidationResult(
                "Forecast data range cannot exceed 1095 days.",
                [nameof(FromDate), nameof(ToDate)]);
        }
    }
}
