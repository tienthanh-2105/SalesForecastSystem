using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesForecastSystem.Infrastructure.Entities;

namespace SalesForecastSystem.Infrastructure.Data.Configurations;

public sealed class InventoryTransactionConfiguration : IEntityTypeConfiguration<InventoryTransaction>
{
    public void Configure(EntityTypeBuilder<InventoryTransaction> builder)
    {
        builder.ToTable("InventoryTransactions", "dbo");
        builder.HasKey(x => x.InventoryTransactionId);
        builder.Property(x => x.TransactionDate).HasColumnType("date");
    }
}
