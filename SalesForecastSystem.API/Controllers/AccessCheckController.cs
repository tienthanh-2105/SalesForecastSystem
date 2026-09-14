using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesForecastSystem.Core.Helpers;

namespace SalesForecastSystem.API.Controllers
{
    [ApiController]
    [Route("api/access-check")]
    public class AccessCheckController : ControllerBase
    {
        [HttpGet("admin")]
        [Authorize(Roles = RoleNames.Admin)]
        public IActionResult Admin()
        {
            return Ok(new
            {
                message = "Bạn có quyền Admin."
            });
        }

        [HttpGet("warehouse")]
        [Authorize(Roles = RoleNames.QuanLyKho)]
        public IActionResult Warehouse()
        {
            return Ok(new
            {
                message = "Bạn có quyền Quản lý kho."
            });
        }

        [HttpGet("sales")]
        [Authorize(Roles = RoleNames.NhanVienBanHang)]
        public IActionResult Sales()
        {
            return Ok(new
            {
                message = "Bạn có quyền Nhân viên bán hàng."
            });
        }
    }
}
