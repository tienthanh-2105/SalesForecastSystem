using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesForecastSystem.Infrastructure.Entities;

namespace SalesForecastSystem.Infrastructure.Data.Configurations;

public class PhienDangNhapConfiguration : IEntityTypeConfiguration<PhienDangNhap>
{
    public void Configure(EntityTypeBuilder<PhienDangNhap> builder)
    {
        builder.ToTable("PhienDangNhap", "dbo");
        builder.HasKey(x => x.MaPhien);
        builder.Property(x => x.MaPhien).ValueGeneratedNever();
        builder.HasIndex(x => new { x.MaNgDung, x.HetHanLuc });
        builder.HasOne(x => x.NguoiDung).WithMany().HasForeignKey(x => x.MaNgDung)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
