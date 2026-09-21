using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesForecastSystem.Infrastructure.Entities;

namespace SalesForecastSystem.Infrastructure.Data.Configurations;

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles", "dbo");
        builder.HasKey(role => role.RoleId);
        builder.Property(role => role.Name).HasMaxLength(50).IsRequired();
        builder.Property(role => role.Description).HasMaxLength(255);
        builder.Property(role => role.CreatedAt).HasColumnType("datetime");
        builder.HasIndex(role => role.Name).IsUnique();
    }
}
