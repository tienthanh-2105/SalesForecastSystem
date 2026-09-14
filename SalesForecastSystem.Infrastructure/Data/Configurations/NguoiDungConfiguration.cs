using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesForecastSystem.Infrastructure.Entities;

namespace SalesForecastSystem.Infrastructure.Data.Configurations
{
    public class NguoiDungConfiguration : IEntityTypeConfiguration<NguoiDung>
    {
        public void Configure(EntityTypeBuilder<NguoiDung> builder)
        {
            builder.ToTable("NguoiDung", "dbo", table =>
            {
                table.HasCheckConstraint(
                    "CK_NguoiDung_TrangThai",
                    "[TrangThai] IN (N'Hoạt động', N'Bị khóa')");
            });

            builder.HasKey(x => x.MaNgDung);

            builder.Property(x => x.MaNgDung)
                .UseIdentityColumn();

            builder.Property(x => x.HoTen)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.Email)
                .HasMaxLength(100)
                .IsUnicode(false)
                .IsRequired();

            builder.HasIndex(x => x.Email)
                .IsUnique();

            builder.Property(x => x.MatKhau)
                .HasMaxLength(255)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(x => x.SoDienThoai)
                .HasMaxLength(15)
                .IsUnicode(false);

            builder.Property(x => x.TrangThai)
                .HasMaxLength(20)
                .HasDefaultValue("Hoạt động")
                .IsRequired();

            builder.Property(x => x.NgayTao)
                .HasColumnType("datetime")
                .IsRequired();

            builder.Property(x => x.NgayCapNhat)
                .HasColumnType("datetime");

            builder.HasIndex(x => x.MaVaiTro);
            builder.HasIndex(x => x.TrangThai);

            builder.HasOne(x => x.VaiTro)
                .WithMany()
                .HasForeignKey(x => x.MaVaiTro)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
