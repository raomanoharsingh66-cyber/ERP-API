using BizFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BizFlow.Infrastructure.Data.Configurations;

public class GoodsReceiptNoteConfiguration : IEntityTypeConfiguration<GoodsReceiptNote>
{
    public void Configure(EntityTypeBuilder<GoodsReceiptNote> builder)
    {
        builder.ToTable("GoodsReceiptNotes");

        builder.HasKey(g => g.Id);

        builder.Property(g => g.GRNNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(g => g.SupplierDeliveryNoteNo)
            .HasMaxLength(100);

        builder.Property(g => g.Notes)
            .HasMaxLength(1000);

        builder.HasIndex(g => new { g.BusinessId, g.GRNNumber })
            .IsUnique();

        builder.HasOne(g => g.Business)
            .WithMany()
            .HasForeignKey(g => g.BusinessId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(g => g.Supplier)
            .WithMany()
            .HasForeignKey(g => g.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(g => g.PurchaseOrder)
            .WithMany(p => p.GoodsReceipts)
            .HasForeignKey(g => g.PurchaseOrderId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(g => g.Warehouse)
            .WithMany()
            .HasForeignKey(g => g.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(g => g.Items)
            .WithOne(i => i.GoodsReceiptNote)
            .HasForeignKey(i => i.GoodsReceiptNoteId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
