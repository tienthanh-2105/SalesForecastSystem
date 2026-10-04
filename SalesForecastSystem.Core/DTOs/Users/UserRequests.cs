using System.ComponentModel.DataAnnotations;
using System.Text;
using SalesForecastSystem.Core.Helpers;

namespace SalesForecastSystem.Core.DTOs.Users;

public sealed class UserQueryRequest : IValidatableObject
{
    [StringLength(200, ErrorMessage = "Search cannot exceed 200 characters.")]
    public string? Search { get; set; }

    public string? Role { get; set; }
    public string? Status { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Page must be at least 1.")]
    public int Page { get; set; } = 1;

    [Range(1, 100, ErrorMessage = "Page size must be between 1 and 100.")]
    public int PageSize { get; set; } = 20;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.IsNullOrWhiteSpace(Role) &&
            !RoleNames.Values.Contains(Role.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            yield return new ValidationResult("Role is invalid.", [nameof(Role)]);
        }

        if (!string.IsNullOrWhiteSpace(Status) &&
            !UserStatusValues.Values.Contains(Status.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            yield return new ValidationResult("Status is invalid.", [nameof(Status)]);
        }
    }
}

public class UserWriteRequest
{
    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters.")]
    [RegularExpression(AccountValidation.EmailPattern, ErrorMessage = "Email không đúng định dạng (ví dụ: ten@example.com).")]
    public string Email { get; set; } = string.Empty;

    [StringLength(15, ErrorMessage = "Phone number cannot exceed 15 characters.")]
    [RegularExpression(@"[0-9+(). -]+", ErrorMessage = "Phone number contains invalid characters.")]
    public string? PhoneNumber { get; set; }

    [Required(ErrorMessage = "Role is required.")]
    public string Role { get; set; } = string.Empty;
}

public sealed class UserCreateRequest : UserWriteRequest, IValidatableObject
{
    [Required(ErrorMessage = "Password is required.")]
    [MinLength(12, ErrorMessage = "Password must contain at least 12 characters.")]
    [RegularExpression(AccountValidation.PasswordPattern, ErrorMessage = "Mật khẩu phải có ít nhất một chữ cái (hoa hoặc thường) và một chữ số.")]
    public string Password { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) =>
        UserRequestValidation.Validate(FullName, Email, Role, Password);
}

public sealed class UserUpdateRequest : UserWriteRequest, IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) =>
        UserRequestValidation.Validate(FullName, Email, Role);
}

public sealed class UserStatusRequest : IValidatableObject
{
    [Required(ErrorMessage = "Status is required.")]
    public string Status { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Status) ||
            !UserStatusValues.Values.Contains(Status.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            yield return new ValidationResult("Status is invalid.", [nameof(Status)]);
        }
    }
}

public sealed class ResetPasswordRequest : IValidatableObject
{
    [Required(ErrorMessage = "New password is required.")]
    [MinLength(12, ErrorMessage = "New password must contain at least 12 characters.")]
    [RegularExpression(AccountValidation.PasswordPattern, ErrorMessage = "Mật khẩu phải có ít nhất một chữ cái (hoa hoặc thường) và một chữ số.")]
    public string NewPassword { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.IsNullOrEmpty(NewPassword) && Encoding.UTF8.GetByteCount(NewPassword) > 72)
        {
            yield return new ValidationResult(
                "New password cannot exceed 72 UTF-8 bytes.",
                [nameof(NewPassword)]);
        }
    }
}

public static class UserStatusValues
{
    public const string Active = "Active";
    public const string Locked = "Locked";
    public static readonly string[] Values = [Active, Locked];
}

internal static class UserRequestValidation
{
    public static IEnumerable<ValidationResult> Validate(
        string fullName,
        string email,
        string role,
        string? password = null)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            yield return new ValidationResult("Full name is required.", [nameof(UserWriteRequest.FullName)]);
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            yield return new ValidationResult("Email is required.", [nameof(UserWriteRequest.Email)]);
        }
        else if (!new EmailAddressAttribute().IsValid(email.Trim()))
        {
            yield return new ValidationResult("Email is invalid.", [nameof(UserWriteRequest.Email)]);
        }

        if (string.IsNullOrWhiteSpace(role) ||
            !RoleNames.Values.Contains(role.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            yield return new ValidationResult("Role is invalid.", [nameof(UserWriteRequest.Role)]);
        }

        if (password is not null && Encoding.UTF8.GetByteCount(password) > 72)
        {
            yield return new ValidationResult("Password cannot exceed 72 UTF-8 bytes.", [nameof(UserCreateRequest.Password)]);
        }
    }
}
