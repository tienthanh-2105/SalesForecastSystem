using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SalesForecastSystem.Core.DTOs.Auth;
using SalesForecastSystem.Core.Interfaces.Services;
using SalesForecastSystem.Infrastructure.Data;
using SalesForecastSystem.Infrastructure.Entities;

namespace SalesForecastSystem.API.Services
{
    public class JwtTokenService : ITokenService
    {
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _context;

        public JwtTokenService(IConfiguration configuration, AppDbContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        public async Task<LoginResponse> CreateAccessTokenAsync(LoginUserResponse user, CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var expiresAt = now.AddMinutes(15);
            var sessionId = Guid.NewGuid();

            var claims = new Claim[]
            {
            new Claim(
                "sub",
                user.MaNgDung.ToString(CultureInfo.InvariantCulture)),

            new Claim("jti", sessionId.ToString()),
            new Claim("email", user.Email),
            new Claim("name", user.HoTen),
            new Claim("role", user.VaiTro)
            };

            var jwtKey = _configuration["Jwt:Key"]
                ?? throw new InvalidOperationException(
                    "Chưa cấu hình Jwt:Key.");

            var issuer = _configuration["Jwt:Issuer"]
                ?? throw new InvalidOperationException(
                    "Chưa cấu hình Jwt:Issuer.");

            var audience = _configuration["Jwt:Audience"]
                ?? throw new InvalidOperationException(
                    "Chưa cấu hình Jwt:Audience.");

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey));

            var credentials = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                notBefore: now,
                expires: expiresAt,
                signingCredentials: credentials);

            _context.PhienDangNhaps.Add(new PhienDangNhap
            {
                MaPhien = sessionId,
                MaNgDung = user.MaNgDung,
                NgayTao = now,
                HetHanLuc = expiresAt
            });
            await _context.SaveChangesAsync(cancellationToken);

            return new LoginResponse
            {
                AccessToken = new JwtSecurityTokenHandler()
                    .WriteToken(token),

                TokenType = "Bearer",
                ExpiresAt = expiresAt,
                User = user
            };
        }
    }
}
