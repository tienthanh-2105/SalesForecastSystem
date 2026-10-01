using System.ComponentModel.DataAnnotations;

namespace SalesForecastSystem.Core.DTOs.Customers;

public sealed class CustomerQueryRequest
{
    [StringLength(200)] public string? Search { get; set; }
    public bool? IsActive { get; set; }
    [Range(1, int.MaxValue)] public int Page { get; set; } = 1;
    [Range(1, 100)] public int PageSize { get; set; } = 20;
}

public sealed class CustomerRequest : IValidatableObject
{
    [Required, StringLength(100)] public string FullName { get; set; } = string.Empty;
    [StringLength(100), EmailAddress] public string? Email { get; set; }
    [StringLength(15), RegularExpression(@"[0-9+(). -]+")]
    public string? PhoneNumber { get; set; }
    [StringLength(255)] public string? Address { get; set; }
    public bool IsActive { get; set; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(FullName))
            yield return new ValidationResult("Full name is required.", [nameof(FullName)]);
        if (Email is not null && string.IsNullOrWhiteSpace(Email))
            yield return new ValidationResult("Email cannot be blank.", [nameof(Email)]);
        if (PhoneNumber is not null && string.IsNullOrWhiteSpace(PhoneNumber))
            yield return new ValidationResult("Phone number cannot be blank.", [nameof(PhoneNumber)]);
    }
}
