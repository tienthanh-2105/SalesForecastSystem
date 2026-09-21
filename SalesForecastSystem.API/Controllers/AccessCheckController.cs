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
        public IActionResult CheckAdminAccess()
        {
            return Ok(new
            {
                message = "Bạn có quyền Admin."
            });
        }

        [HttpGet("warehouse")]
        [Authorize(Roles = RoleNames.WarehouseManager)]
        public IActionResult CheckWarehouseAccess()
        {
            return Ok(new
            {
                message = "Bạn có quyền Quản lý kho."
            });
        }

        [HttpGet("sales")]
        [Authorize(Roles = RoleNames.SalesStaff)]
        public IActionResult CheckSalesAccess()
        {
            return Ok(new
            {
                message = "Bạn có quyền Nhân viên bán hàng."
            });
        }
    }
}
