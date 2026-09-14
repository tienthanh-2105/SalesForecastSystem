using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SalesForecastSystem.Infrastructure.Data;
using SalesForecastSystem.Infrastructure.Entities;

namespace SalesForecastSystem.Infrastructure.Seeders
{
    public static class DataSeeder
    {
        public static async Task SeedAdminAsync(
            AppDbContext context,
            string email,
            string password)
        {
            email = email.Trim().ToLowerInvariant();

            // Không tạo lại hoặc đổi mật khẩu tài khoản đã có.
            if (await context.NguoiDungs.AnyAsync(x => x.Email == email))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(password)
                || password == "THAY_BANG_MAT_KHAU_RIENG"
                || password.Length < 12
                || System.Text.Encoding.UTF8.GetByteCount(password) > 72)
            {
                throw new InvalidOperationException(
                    "Hãy đặt mật khẩu Admin riêng, ít nhất 12 ký tự " +
                    "và không quá 72 byte UTF-8.");
            }

            using var transaction =
                await context.Database.BeginTransactionAsync();

            var vaiTro = await context.VaiTros
                .SingleOrDefaultAsync(x => x.TenVaiTro == "Admin");

            if (vaiTro is null)
            {
                vaiTro = new VaiTro
                {
                    TenVaiTro = "Admin",
                    MoTa = "Quản trị hệ thống",
                    TrangThai = true,
                    NgayTao = DateTime.Now
                };

                context.VaiTros.Add(vaiTro);
            }

            var admin = new NguoiDung
            {
                VaiTro = vaiTro,
                HoTen = "Quản trị viên",
                Email = email,
                MatKhau = BCrypt.Net.BCrypt.HashPassword(
                    password, workFactor: 12),
                TrangThai = "Hoạt động",
                NgayTao = DateTime.Now
            };

            context.NguoiDungs.Add(admin);

            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
    }
}
