using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace SalesForecastSystem.Core.DTOs.Products;

public class ProductRequest : IValidatableObject
{
    [Required(ErrorMessage = "SKU is required.")]
    [StringLength(50, ErrorMessage = "SKU cannot exceed 50 characters.")]
    [RegularExpression(@"[\x20-\x7E]+", ErrorMessage = "SKU can only contain printable ASCII characters.")]
    public string SKU { get; set; } = string.Empty;

    [Required(ErrorMessage = "Product name is required.")]
    [StringLength(200, ErrorMessage = "Product name cannot exceed 200 characters.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Unit is required.")]
    [StringLength(30, ErrorMessage = "Unit cannot exceed 30 characters.")]
    public string Unit { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
    public string? Description { get; set; }

    [StringLength(2048, ErrorMessage = "Image URL cannot exceed 2048 characters.")]
    public string? ImageUrl { get; set; }

    [Required(ErrorMessage = "Category is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Category ID must be a positive integer.")]
    public int? CategoryId { get; set; }

    [Required(ErrorMessage = "Sale price is required.")]
    public decimal? SalePrice { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Minimum stock level must be a non-negative integer.")]
    public int MinimumStockLevel { get; set; }

    public bool IsActive { get; set; } = true;

    public JsonElement Quantity { get; set; }
    public JsonElement QuantityOnHand { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var (name, value) in new[]
                 {
                     (nameof(Quantity), Quantity),
                     (nameof(QuantityOnHand), QuantityOnHand)
                 })
        {
            if (value.ValueKind == JsonValueKind.Undefined)
            {
                continue;
            }

            var message = value.ValueKind != JsonValueKind.Number ||
                          !value.TryGetInt64(out var quantity) ||
                          quantity < 0
                ? "Quantity must be a non-negative integer."
                : "Inventory is derived from posted transactions and cannot be edited on a product.";
            yield return new ValidationResult(message, [name]);
        }

        if (SalePrice is decimal price &&
            (price < 0 || price > 9999999999999999.99m || decimal.Round(price, 2) != price))
        {
            yield return new ValidationResult(
                "Sale price must be between 0 and 9999999999999999.99 with at most two decimal places.",
                [nameof(SalePrice)]);
        }

        if (!string.IsNullOrWhiteSpace(ImageUrl) &&
            (!Uri.TryCreate(ImageUrl.Trim(), UriKind.Absolute, out var imageUri) ||
             (imageUri.Scheme != Uri.UriSchemeHttp && imageUri.Scheme != Uri.UriSchemeHttps)))
        {
            yield return new ValidationResult(
                "Image URL must be an absolute HTTP or HTTPS URL.",
                [nameof(ImageUrl)]);
        }
    }
}

public sealed class ProductUpdateRequest : ProductRequest
{
    [Required(ErrorMessage = "Row version is required.")]
    public string RowVersion { get; set; } = string.Empty;
}
