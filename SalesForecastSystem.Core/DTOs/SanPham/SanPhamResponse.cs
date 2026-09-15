namespace SalesForecastSystem.Core.DTOs.SanPham;

public record SanPhamResponse(int MaSanPham, int MaDanhMuc, string SKU, string TenSanPham,
    string DonViTinh, decimal GiaBan, bool TrangThai, DateTime NgayTao);
