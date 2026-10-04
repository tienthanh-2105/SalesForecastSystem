using System.ComponentModel.DataAnnotations;

namespace SalesForecastSystem.Core.DTOs.Suppliers;

public sealed class SupplierQueryRequest
{
    [StringLength(200)]
    public string? Search { get; set; }
    public bool? IsActive { get; set; }
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;
    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
}

public sealed class SupplierRequest : IValidatableObject
{
    [Required, StringLength(200)]
    public string Name { get; set; } = string.Empty;
    [Required(ErrorMessage = "Vui lòng nhập mã số thuế doanh nghiệp.")]
    [StringLength(14)]
    [RegularExpression(@"[0-9]{10}(-[0-9]{3})?", ErrorMessage = "Mã số thuế phải gồm 10 chữ số hoặc có dạng 0123456789-001.")]
    public string? TaxCode { get; set; }
    [StringLength(100), EmailAddress]
    public string? Email { get; set; }
    [StringLength(15), RegularExpression(@"[0-9+(). -]+")]
    public string? PhoneNumber { get; set; }
    [StringLength(255)]
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Name))
            yield return new ValidationResult("Name is required.", [nameof(Name)]);
        if (TaxCode is not null && string.IsNullOrWhiteSpace(TaxCode))
            yield return new ValidationResult("Tax code cannot be blank.", [nameof(TaxCode)]);
    }
}
