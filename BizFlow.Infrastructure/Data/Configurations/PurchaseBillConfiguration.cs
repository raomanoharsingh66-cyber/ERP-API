using BizFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BizFlow.Infrastructure.Data.Configurations;

public class PurchaseBillConfiguration : IEntityTypeConfiguration<PurchaseBill>
{
    public void Configure(EntityTypeBuilder<PurchaseBill> builder)
    {
        builder.ToTable("PurchaseBills");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.BillNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(b => b.VendorInvoiceNumber)
            .HasMaxLength(100);

        builder.Property(b => b.SubTotal)
            .HasColumnType("decimal(18,2)");

        builder.Property(b => b.TaxAmount)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0m);

        builder.Property(b => b.CgstAmount)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0m);

        builder.Property(b => b.SgstAmount)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0m);

        builder.Property(b => b.IgstAmount)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0m);

        builder.Property(b => b.TotalAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(b => b.PaidAmount)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0m);

        builder.Property(b => b.BalanceAmount)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0m);

        builder.Property(b => b.Notes)
            .HasMaxLength(1000);

        builder.HasIndex(b => new { b.BusinessId, b.BillNumber })
            .IsUnique();

        builder.HasOne(b => b.Business)
            .WithMany()
            .HasForeignKey(b => b.BusinessId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Supplier)
            .WithMany(s => s.PurchaseBills)
            .HasForeignKey(b => b.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.PurchaseOrder)
            .WithMany(p => p.Bills)
            .HasForeignKey(b => b.PurchaseOrderId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(b => b.Warehouse)
            .WithMany()
            .HasForeignKey(b => b.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(b => b.Items)
            .WithOne(i => i.PurchaseBill)
            .HasForeignKey(i => i.PurchaseBillId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(b => b.Payments)
            .WithOne(p => p.PurchaseBill)
            .HasForeignKey(p => p.PurchaseBillId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
