using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using SalesForecastSystem.Core.Helpers;
using SalesForecastSystem.Infrastructure.Data;

namespace SalesForecastSystem.API.Services;

public class SessionJwtEvents(AppDbContext context) : JwtBearerEvents
{
    public override async Task TokenValidated(TokenValidatedContext validation)
    {
        var principal = validation.Principal;
        if (!Guid.TryParse(principal?.FindFirst("jti")?.Value, out var sessionId)
            || !int.TryParse(principal?.FindFirst("sub")?.Value, out var userId))
        {
            validation.Fail("Phiên đăng nhập không hợp lệ.");
            return;
        }

        var session = await context.PhienDangNhaps.AsNoTracking()
            .Include(x => x.NguoiDung).ThenInclude(x => x.VaiTro)
            .SingleOrDefaultAsync(x => x.MaPhien == sessionId && x.MaNgDung == userId,
                validation.HttpContext.RequestAborted);

        if (session is null || session.ThuHoiLuc is not null || session.HetHanLuc <= DateTime.UtcNow
            || session.NguoiDung.TrangThai != "Hoạt động" || !session.NguoiDung.VaiTro.TrangThai
            || RoleNames.Normalize(session.NguoiDung.VaiTro.TenVaiTro) != principal!.FindFirst("role")?.Value)
        {
            validation.Fail("Phiên đã hết hạn, bị thu hồi hoặc quyền tài khoản đã thay đổi.");
        }
    }
}
