using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SalesForecastSystem.Core.DTOs.Auth;
using SalesForecastSystem.Core.Interfaces.Services;
using SalesForecastSystem.Infrastructure.Data;
using SalesForecastSystem.Core.Helpers;

namespace SalesForecastSystem.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly ITokenService _tokenService;

        public AuthService(
            AppDbContext context,
            ITokenService tokenService)
        {
            _context = context;
            _tokenService = tokenService;
        }

        public async Task<LoginResponse?> LoginAsync(
            LoginRequest request,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Email)
                || string.IsNullOrEmpty(request.Password)
                || Encoding.UTF8.GetByteCount(request.Password) > 72)
            {
                return null;
            }

            var email = request.Email.Trim().ToLowerInvariant();

            var user = await _context.NguoiDungs
                .AsNoTracking()
                .Include(x => x.VaiTro)
                .SingleOrDefaultAsync(
                    x => x.Email == email,
                    cancellationToken);

            // Kiểm tra tài khoản và trạng thái vai trò.
            if (user is null
                || user.TrangThai != "Hoạt động"
                || user.VaiTro is null
                || !user.VaiTro.TrangThai)
            {
                return null;
            }

            // Đối chiếu mật khẩu nhập với bản băm trong database.
            bool passwordMatches;
            try
            {
                passwordMatches = BCrypt.Net.BCrypt.Verify(request.Password, user.MatKhau);
            }
            catch (BCrypt.Net.SaltParseException)
            {
                return null;
            }

            if (!passwordMatches)
            {
                return null;
            }

            // Chuẩn hóa tên vai trò để dùng khi phân quyền.
            var role = RoleNames.Normalize(user.VaiTro.TenVaiTro);

            var userInfo = new LoginUserResponse
            {
                MaNgDung = user.MaNgDung,
                HoTen = user.HoTen,
                Email = user.Email,
                VaiTro = role
            };

            return await _tokenService.CreateAccessTokenAsync(userInfo, cancellationToken);
        }
    }
}
