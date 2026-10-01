using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesForecastSystem.Infrastructure.Entities;

namespace SalesForecastSystem.Infrastructure.Data.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories", "dbo");
        builder.HasKey(category => category.CategoryId);
        builder.Property(category => category.Code).HasMaxLength(20).IsUnicode(false).IsRequired();
        builder.Property(category => category.Name).HasMaxLength(100).IsRequired();
        builder.Property(category => category.Description).HasMaxLength(500);
        builder.HasIndex(category => category.Name).IsUnique();
        builder.HasIndex(category => category.Code).IsUnique();
        builder.HasIndex(category => category.ParentCategoryId);
        builder.HasOne(category => category.ParentCategory)
            .WithMany(category => category.Children)
            .HasForeignKey(category => category.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
