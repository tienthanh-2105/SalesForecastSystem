using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SalesForecastSystem.Infrastructure.Entities;

namespace SalesForecastSystem.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<VaiTro> VaiTros => Set<VaiTro>();

        public DbSet<NguoiDung> NguoiDungs => Set<NguoiDung>();
        public DbSet<PhienDangNhap> PhienDangNhaps => Set<PhienDangNhap>();
        public DbSet<DanhMuc> DanhMucs => Set<DanhMuc>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(
                typeof(AppDbContext).Assembly);
        }
    }
}
