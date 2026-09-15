using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace SalesForecastSystem.Core.DTOs.SanPham;

public class SanPhamRequest : IValidatableObject
{
    [Required(ErrorMessage = "Mã SKU không được để trống.")]
    [StringLength(50, ErrorMessage = "Mã SKU tối đa 50 ký tự.")]
    [RegularExpression(@"[\x20-\x7E]+", ErrorMessage = "Mã SKU chỉ được chứa ký tự ASCII in được.")]
    public string SKU { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tên sản phẩm không được để trống.")]
    [StringLength(200, ErrorMessage = "Tên sản phẩm tối đa 200 ký tự.")]
    public string TenSanPham { get; set; } = string.Empty;

    [Required(ErrorMessage = "Đơn vị tính không được để trống.")]
    [StringLength(30, ErrorMessage = "Đơn vị tính tối đa 30 ký tự.")]
    public string DonViTinh { get; set; } = string.Empty;

    [Required(ErrorMessage = "Danh mục không được để trống.")]
    [Range(1, int.MaxValue, ErrorMessage = "Mã danh mục phải là số nguyên dương.")]
    public int? MaDanhMuc { get; set; }

    [Required(ErrorMessage = "Giá bán không được để trống.")]
    public decimal? GiaBan { get; set; }
    public bool TrangThai { get; set; } = true;

    // Capture attempted stock edits instead of silently ignoring them during JSON binding.
    public JsonElement SoLuong { get; set; }
    public JsonElement SoLuongTon { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var (name, value) in new[] { (nameof(SoLuong), SoLuong), (nameof(SoLuongTon), SoLuongTon) })
        {
            if (value.ValueKind == JsonValueKind.Undefined) continue;
            var message = value.ValueKind != JsonValueKind.Number || !value.TryGetInt64(out var quantity) || quantity < 0
                ? "Số lượng phải là số nguyên không âm."
                : "Tồn kho được tính từ phiếu nhập/bán; không được sửa số lượng trực tiếp trên sản phẩm.";
            yield return new ValidationResult(message, [name]);
        }
        if (GiaBan is decimal price && (price < 0 || price > 9999999999999999.99m || decimal.Round(price, 2) != price))
            yield return new ValidationResult("Giá bán phải từ 0 đến 9999999999999999.99 và có tối đa 2 chữ số thập phân.", [nameof(GiaBan)]);
    }
}
