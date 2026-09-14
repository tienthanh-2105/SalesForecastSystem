using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesForecastSystem.Infrastructure.Entities;

namespace SalesForecastSystem.Infrastructure.Data.Configurations;

public class DanhMucConfiguration : IEntityTypeConfiguration<DanhMuc>
{
    public void Configure(EntityTypeBuilder<DanhMuc> builder)
    {
        builder.ToTable("DanhMuc", "dbo");
        builder.HasKey(x => x.MaDanhMuc);
        builder.Property(x => x.TenDanhMuc).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => x.TenDanhMuc).IsUnique();
        builder.Property(x => x.MoTa).HasMaxLength(500);
        // Do not configure a generated default for bool: explicit false must be saved.
        builder.Property(x => x.TrangThai).IsRequired();
    }
}
