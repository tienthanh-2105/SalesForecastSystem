using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesForecastSystem.Infrastructure.Entities;

namespace SalesForecastSystem.Infrastructure.Data.Configurations;

public class SanPhamConfiguration : IEntityTypeConfiguration<SanPham>
{
    public void Configure(EntityTypeBuilder<SanPham> builder)
    {
        builder.ToTable("SanPham", "dbo");
        builder.HasKey(x => x.MaSanPham);
        builder.Property(x => x.SKU).HasMaxLength(50).IsUnicode(false).IsRequired();
        builder.HasIndex(x => x.SKU).IsUnique();
        builder.Property(x => x.TenSanPham).HasMaxLength(200).IsRequired();
        builder.Property(x => x.DonViTinh).HasMaxLength(30).IsRequired();
        builder.Property(x => x.GiaBan).HasPrecision(18, 2);
        builder.Property(x => x.NgayTao).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(x => x.PhienBan).IsRowVersion();
        builder.HasOne<DanhMuc>().WithMany().HasForeignKey(x => x.MaDanhMuc).OnDelete(DeleteBehavior.Restrict);
    }
}
