using System.ComponentModel.DataAnnotations;

namespace SalesForecastSystem.Core.DTOs.DanhMuc;

public class DanhMucRequest
{
    [Required(ErrorMessage = "Tên danh mục không được để trống.")]
    [StringLength(100, ErrorMessage = "Tên danh mục tối đa 100 ký tự.")]
    public string TenDanhMuc { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Mô tả tối đa 500 ký tự.")]
    public string? MoTa { get; set; }
    public bool TrangThai { get; set; } = true;
}
