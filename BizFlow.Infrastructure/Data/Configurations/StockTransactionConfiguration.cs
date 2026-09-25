using BizFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BizFlow.Infrastructure.Data.Configurations;

public class StockTransactionConfiguration : IEntityTypeConfiguration<StockTransaction>
{
    public void Configure(EntityTypeBuilder<StockTransaction> builder)
    {
        builder.ToTable("StockTransactions");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Quantity)
            .HasColumnType("decimal(18,3)")
            .IsRequired();

        builder.Property(t => t.UnitPrice)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0m);

        builder.Property(t => t.TotalAmount)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0m);

        builder.Property(t => t.ReferenceType)
            .HasMaxLength(50);

        builder.Property(t => t.ReferenceId)
            .HasMaxLength(100);

        builder.Property(t => t.Notes)
            .HasMaxLength(500);

        builder.HasIndex(t => new { t.BusinessId, t.ProductId });
        builder.HasIndex(t => new { t.BusinessId, t.WarehouseId });
        builder.HasIndex(t => new { t.BusinessId, t.Timestamp });

        builder.HasOne(t => t.Business)
            .WithMany()
            .HasForeignKey(t => t.BusinessId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Product)
            .WithMany(p => p.StockTransactions)
            .HasForeignKey(t => t.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Warehouse)
            .WithMany(w => w.StockTransactions)
            .HasForeignKey(t => t.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
