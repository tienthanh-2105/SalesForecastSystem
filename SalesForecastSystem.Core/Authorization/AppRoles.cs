namespace SalesForecastSystem.Core.Authorization;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string QuanLyKho = "QuanLyKho";
    public const string NhanVienBanHang = "NhanVienBanHang";
    public const string All = Admin + "," + QuanLyKho + "," + NhanVienBanHang;

    public static string Normalize(string role) => role switch
    {
        "Quản lý kho" => QuanLyKho,
        "Nhân viên bán hàng" => NhanVienBanHang,
        _ => role
    };
}
