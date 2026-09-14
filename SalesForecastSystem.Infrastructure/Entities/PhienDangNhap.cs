namespace SalesForecastSystem.Infrastructure.Entities;

public class PhienDangNhap
{
    public Guid MaPhien { get; set; }
    public int MaNgDung { get; set; }
    public DateTime NgayTao { get; set; }
    public DateTime HetHanLuc { get; set; }
    public DateTime? ThuHoiLuc { get; set; }
    public NguoiDung NguoiDung { get; set; } = null!;
}
