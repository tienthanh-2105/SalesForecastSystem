using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesForecastSystem.Infrastructure.Entities;

namespace SalesForecastSystem.Infrastructure.Data.Configurations;

public sealed class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("Suppliers", "dbo");
        builder.HasKey(item => item.SupplierId);
        builder.Property(item => item.Name).HasMaxLength(200).IsRequired();
        builder.Property(item => item.TaxCode).IsUnicode(false).HasMaxLength(20);
        builder.Property(item => item.Email).IsUnicode(false).HasMaxLength(100);
        builder.Property(item => item.PhoneNumber).IsUnicode(false).HasMaxLength(15);
        builder.Property(item => item.Address).HasMaxLength(255);
        builder.HasIndex(item => item.TaxCode).IsUnique().HasFilter("[TaxCode] IS NOT NULL");
    }
}
