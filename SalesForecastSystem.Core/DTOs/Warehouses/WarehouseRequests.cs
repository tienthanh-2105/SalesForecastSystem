using System.ComponentModel.DataAnnotations;

namespace SalesForecastSystem.Core.DTOs.Warehouses;

public sealed class WarehouseQueryRequest
{
    [StringLength(200)]
    public string? Search { get; set; }
    public bool? IsActive { get; set; }
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;
    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
}

public sealed class WarehouseRequest : IValidatableObject
{
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;
    [StringLength(255)]
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Name))
            yield return new ValidationResult("Name is required.", [nameof(Name)]);
    }
}
