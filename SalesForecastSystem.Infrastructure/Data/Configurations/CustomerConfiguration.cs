using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesForecastSystem.Infrastructure.Entities;

namespace SalesForecastSystem.Infrastructure.Data.Configurations;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers", "dbo");
        builder.HasKey(item => item.CustomerId);
        builder.Property(item => item.FullName).HasMaxLength(100).IsRequired();
        builder.Property(item => item.Email).IsUnicode(false).HasMaxLength(100);
        builder.Property(item => item.PhoneNumber).IsUnicode(false).HasMaxLength(15);
        builder.Property(item => item.Address).HasMaxLength(255);
        builder.Property(item => item.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(item => item.IsActive).HasDefaultValue(true);
        builder.HasIndex(item => item.Email).IsUnique().HasFilter("[Email] IS NOT NULL");
    }
}
