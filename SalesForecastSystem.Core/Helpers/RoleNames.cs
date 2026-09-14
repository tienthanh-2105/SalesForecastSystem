using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesForecastSystem.Core.Helpers
{
    public static class RoleNames
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
}
