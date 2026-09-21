using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesForecastSystem.Infrastructure.Entities;

namespace SalesForecastSystem.Infrastructure.Data.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products", "dbo");
        builder.HasKey(product => product.ProductId);
        builder.Property(product => product.SKU).HasMaxLength(50).IsUnicode(false).IsRequired();
        builder.Property(product => product.Name).HasMaxLength(200).IsRequired();
        builder.Property(product => product.Unit).HasMaxLength(30).IsRequired();
        builder.Property(product => product.Description).HasMaxLength(1000);
        builder.Property(product => product.ImageUrl).HasMaxLength(2048).IsUnicode(false);
        builder.Property(product => product.SalePrice).HasPrecision(18, 2);
        builder.Property(product => product.MinimumStockLevel).HasDefaultValue(0);
        builder.Property(product => product.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(product => product.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(product => product.RowVersion).IsRowVersion();
        builder.HasIndex(product => product.SKU).IsUnique();
        builder.HasIndex(product => product.CategoryId);
        builder.HasIndex(product => new { product.IsActive, product.CategoryId });
        builder.HasOne(product => product.Category)
            .WithMany(category => category.Products)
            .HasForeignKey(product => product.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
