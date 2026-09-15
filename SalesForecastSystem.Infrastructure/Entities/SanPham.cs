namespace SalesForecastSystem.Infrastructure.Entities;

public class SanPham
{
    public int MaSanPham { get; set; }
    public int MaDanhMuc { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string TenSanPham { get; set; } = string.Empty;
    public string DonViTinh { get; set; } = string.Empty;
    public decimal GiaBan { get; set; }
    public bool TrangThai { get; set; } = true;
    public DateTime NgayTao { get; set; }
    public byte[] PhienBan { get; set; } = [];
}
