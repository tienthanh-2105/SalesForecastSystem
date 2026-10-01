using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesForecastSystem.Infrastructure.Entities;

namespace SalesForecastSystem.Infrastructure.Data.Configurations;

public sealed class SalesOrderConfiguration : IEntityTypeConfiguration<SalesOrder>
{
    public void Configure(EntityTypeBuilder<SalesOrder> builder)
    {
        builder.ToTable("SalesOrders", "dbo", table => table.HasTrigger("TR_SalesOrders_RequireDraftOnInsert"));
        builder.HasKey(item => item.SalesOrderId);
        builder.Property(item => item.OrderNumber).IsUnicode(false).HasMaxLength(50);
        builder.Property(item => item.OrderDate).HasColumnType("date");
        builder.Property(item => item.Status).IsUnicode(false).HasMaxLength(20);
        builder.Property(item => item.Status).HasDefaultValue("Draft");
        builder.Property(item => item.Notes).HasMaxLength(500);
        builder.Property(item => item.CustomerName).HasMaxLength(100);
        builder.Property(item => item.CustomerPhone).IsUnicode(false).HasMaxLength(15);
        builder.Property(item => item.ShippingAddress).HasMaxLength(255);
        builder.Property(item => item.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(item => item.RowVersion).IsRowVersion();
        builder.HasMany(item => item.Items).WithOne().HasForeignKey(item => item.SalesOrderId);
    }
}

public sealed class SalesOrderItemConfiguration : IEntityTypeConfiguration<SalesOrderItem>
{
    public void Configure(EntityTypeBuilder<SalesOrderItem> builder)
    {
        builder.ToTable("SalesOrderItems", "dbo", table => table.HasTrigger("TR_SalesOrderItems_ProtectCompleted"));
        builder.HasKey(x => x.SalesOrderItemId);
        builder.Property(x => x.UnitPrice).HasPrecision(18, 2);
        builder.Property(x => x.Discount).HasPrecision(18, 2).HasDefaultValue(0);
        builder.Property(x => x.LineTotal).HasPrecision(28, 2).HasComputedColumnSql("CONVERT(decimal(28,2), [Quantity] * [UnitPrice] - [Discount])", stored: true);
    }
}
