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
    public class VaiTroConfiguration : IEntityTypeConfiguration<VaiTro>
    {
        public void Configure(EntityTypeBuilder<VaiTro> builder)
        {
            builder.ToTable("VaiTro", "dbo");

            builder.HasKey(x => x.MaVaiTro);

            builder.Property(x => x.MaVaiTro)
                .UseIdentityColumn();

            builder.Property(x => x.TenVaiTro)
                .HasMaxLength(50)
                .IsRequired();

            builder.HasIndex(x => x.TenVaiTro)
                .IsUnique();

            builder.Property(x => x.MoTa)
                .HasMaxLength(255);

            builder.Property(x => x.TrangThai)
                .HasDefaultValue(true)
                .IsRequired();

            builder.Property(x => x.NgayTao)
                .HasColumnType("datetime")
                .IsRequired();
        }
    }
}
