using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalesForecastSystem.Infrastructure.Data;

namespace SalesForecastSystem.API.Controllers
{
    [ApiController]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = SalesForecastSystem.Core.Authorization.AppRoles.Admin)]
    [Route("api/dev-check")]
    public class DevCheckController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;

        public DevCheckController(
            AppDbContext context,
            IWebHostEnvironment environment,
            IConfiguration configuration)
        {
            _context = context;
            _environment = environment;
            _configuration = configuration;
        }

        [HttpGet("admin")]
        public async Task<IActionResult> CheckAdmin()
        {
            if (!_environment.IsDevelopment())
            {
                return NotFound();
            }

            var email = _configuration["SeedAdmin:Email"];

            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest(new
                {
                    message = "Chưa cấu hình SeedAdmin:Email."
                });
            }

            email = email.Trim().ToLowerInvariant();

            var admin = await _context.NguoiDungs
                .AsNoTracking()
                .Where(x => x.Email == email)
                .Select(x => new
                {
                    x.MaNgDung,
                    x.HoTen,
                    x.Email,
                    TenVaiTro = x.VaiTro.TenVaiTro,
                    x.TrangThai
                })
                .SingleOrDefaultAsync();

            if (admin is null)
            {
                return NotFound(new
                {
                    message = "Chưa tìm thấy tài khoản Admin."
                });
            }

            return Ok(admin);
        }
    }
}
