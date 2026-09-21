using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesForecastSystem.Infrastructure.Entities;

namespace SalesForecastSystem.Infrastructure.Data.Configurations;

public sealed class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.ToTable("Warehouses", "dbo");
        builder.HasKey(item => item.WarehouseId);
        builder.Property(item => item.Name).HasMaxLength(100).IsRequired();
        builder.Property(item => item.Address).HasMaxLength(255);
        builder.HasIndex(item => item.Name).IsUnique();
    }
}
