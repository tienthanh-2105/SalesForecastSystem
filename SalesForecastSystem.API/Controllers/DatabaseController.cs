using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalesForecastSystem.Infrastructure.Data;

namespace SalesForecastSystem.API.Controllers
{
    [ApiController]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = SalesForecastSystem.Core.Authorization.AppRoles.Admin)]
    [Route("api/[controller]")]
    public class DatabaseController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DatabaseController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("check")]
        public async Task<IActionResult> CheckConnection()
        {
            bool canConnect = await _context.Database.CanConnectAsync();

            return canConnect
                ? Ok(new { message = "Kết nối SQL Server thành công." })
                : StatusCode(500, new { message = "Không thể kết nối SQL Server." });
        }
    }
}
