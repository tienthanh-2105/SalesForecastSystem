using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SalesForecastSystem.Core.DTOs.Auth;
using SalesForecastSystem.Core.Interfaces.Services;
using SalesForecastSystem.Infrastructure.Data;

namespace SalesForecastSystem.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly AppDbContext _context;

        public AuthController(IAuthService authService, AppDbContext context)
        {
            _authService = authService;
            _context = context;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login(
            LoginRequest request,
            CancellationToken cancellationToken)
        {
            Response.Headers["Cache-Control"] = "no-store";

            var result = await _authService.LoginAsync(
                request,
                cancellationToken);

            if (result is null)
            {
                return Unauthorized(new
                {
                    message = "Thông tin đăng nhập không hợp lệ."
                });
            }

            return Ok(result);
        }

        [Authorize]
        [HttpPost("logout")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Logout(CancellationToken cancellationToken)
        {
            var sessionId = Guid.Parse(User.FindFirst("jti")!.Value);
            var userId = int.Parse(User.FindFirst("sub")!.Value);
            await _context.PhienDangNhaps
                .Where(x => x.MaPhien == sessionId && x.MaNgDung == userId && x.ThuHoiLuc == null)
                .ExecuteUpdateAsync(update => update.SetProperty(x => x.ThuHoiLuc, DateTime.UtcNow), cancellationToken);
            Response.Headers["Cache-Control"] = "no-store";
            return Ok(new { message = "Đăng xuất thành công." });
        }

        [Authorize]
        [HttpGet("me")]
        public IActionResult Me()
        {
            Response.Headers["Cache-Control"] = "no-store";
            return Ok(new
            {
                maNgDung = User.FindFirst("sub")?.Value,
                email = User.FindFirst("email")?.Value,
                hoTen = User.Identity?.Name,
                vaiTro = User.FindFirst("role")?.Value
            });
        }
    }
}
