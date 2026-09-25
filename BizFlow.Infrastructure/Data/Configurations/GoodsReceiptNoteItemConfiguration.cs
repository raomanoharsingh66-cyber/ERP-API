using BizFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BizFlow.Infrastructure.Data.Configurations;

public class GoodsReceiptNoteItemConfiguration : IEntityTypeConfiguration<GoodsReceiptNoteItem>
{
    public void Configure(EntityTypeBuilder<GoodsReceiptNoteItem> builder)
    {
        builder.ToTable("GoodsReceiptNoteItems");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.ReceivedQuantity)
            .HasColumnType("decimal(18,3)");

        builder.Property(i => i.AcceptedQuantity)
            .HasColumnType("decimal(18,3)");

        builder.Property(i => i.RejectedQuantity)
            .HasColumnType("decimal(18,3)")
            .HasDefaultValue(0m);

        builder.Property(i => i.UnitPrice)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0m);

        builder.Property(i => i.RejectionReason)
            .HasMaxLength(300);

        builder.Property(i => i.Notes)
            .HasMaxLength(500);

        builder.HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.PurchaseOrderItem)
            .WithMany()
            .HasForeignKey(i => i.PurchaseOrderItemId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
