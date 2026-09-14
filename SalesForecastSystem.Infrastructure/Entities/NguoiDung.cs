using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesForecastSystem.Infrastructure.Entities
{
    public class NguoiDung
    {
        public int MaNgDung { get; set; }
        public int MaVaiTro { get; set; }
        public string HoTen { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string MatKhau { get; set; } = null!;
        public string? SoDienThoai { get; set; }
        public string TrangThai { get; set; } = "Hoạt động";
        public DateTime NgayTao { get; set; }
        public DateTime? NgayCapNhat { get; set; }
        public VaiTro VaiTro { get; set; } = null!;
    }
}
