using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesForecastSystem.Infrastructure.Entities;

namespace SalesForecastSystem.Infrastructure.Data.Configurations;

public sealed class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
    {
        builder.ToTable("PurchaseOrders", "dbo", table => table.HasTrigger("TR_PurchaseOrders_RequireDraftOnInsert"));
        builder.HasKey(x => x.PurchaseOrderId);
        builder.Property(x => x.OrderNumber).IsUnicode(false).HasMaxLength(50);
        builder.Property(x => x.OrderDate).HasColumnType("date");
        builder.Property(x => x.Status).IsUnicode(false).HasMaxLength(20).HasDefaultValue("Draft");
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.PurchaseOrderId);
    }
}

public sealed class PurchaseOrderItemConfiguration : IEntityTypeConfiguration<PurchaseOrderItem>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderItem> builder)
    {
        builder.ToTable("PurchaseOrderItems", "dbo", table => table.HasTrigger("TR_PurchaseOrderItems_ProtectPosted"));
        builder.HasKey(x => x.PurchaseOrderItemId);
        builder.Property(x => x.UnitPrice).HasPrecision(18, 2);
        builder.Property(x => x.LineTotal).HasPrecision(28, 2).HasComputedColumnSql("CONVERT(decimal(28,2), [Quantity] * [UnitPrice])", stored: true);
    }
}
