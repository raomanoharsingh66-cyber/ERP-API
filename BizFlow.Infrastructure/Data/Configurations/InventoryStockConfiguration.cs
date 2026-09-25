using BizFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BizFlow.Infrastructure.Data.Configurations;

public class InventoryStockConfiguration : IEntityTypeConfiguration<InventoryStock>
{
    public void Configure(EntityTypeBuilder<InventoryStock> builder)
    {
        builder.ToTable("InventoryStocks");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.QuantityOnHand)
            .HasColumnType("decimal(18,3)")
            .HasDefaultValue(0m);

        builder.Property(s => s.QuantityReserved)
            .HasColumnType("decimal(18,3)")
            .HasDefaultValue(0m);

        builder.Property(s => s.AverageCost)
            .HasColumnType("decimal(18,4)")
            .HasDefaultValue(0m);

        builder.HasIndex(s => new { s.ProductId, s.WarehouseId })
            .IsUnique();

        builder.HasOne(s => s.Business)
            .WithMany()
            .HasForeignKey(s => s.BusinessId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Product)
            .WithMany(p => p.Stocks)
            .HasForeignKey(s => s.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Warehouse)
            .WithMany(w => w.Stocks)
            .HasForeignKey(s => s.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
