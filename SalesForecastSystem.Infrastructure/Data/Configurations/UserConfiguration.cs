using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesForecastSystem.Infrastructure.Entities;

namespace SalesForecastSystem.Infrastructure.Data.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users", "dbo");
        builder.HasKey(user => user.UserId);
        builder.Property(user => user.Code).HasMaxLength(23).IsUnicode(false).ValueGeneratedOnAddOrUpdate();
        builder.ToTable("Users", "dbo", table => table.UseSqlOutputClause(false));
        builder.Property(user => user.FullName).HasMaxLength(100).IsRequired();
        builder.Property(user => user.Email).HasMaxLength(100).IsUnicode(false).IsRequired();
        builder.Property(user => user.PasswordHash).HasMaxLength(255).IsUnicode(false).IsRequired();
        builder.Property(user => user.PhoneNumber).HasMaxLength(15).IsUnicode(false);
        builder.Property(user => user.Status).HasMaxLength(20).IsUnicode(false).IsRequired();
        builder.Property(user => user.CreatedAt).HasColumnType("datetime");
        builder.Property(user => user.UpdatedAt).HasColumnType("datetime");
        builder.HasIndex(user => user.Email).IsUnique();
        builder.HasIndex(user => user.RoleId);
        builder.HasIndex(user => user.Status);
        builder.HasOne(user => user.Role)
            .WithMany(role => role.Users)
            .HasForeignKey(user => user.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
