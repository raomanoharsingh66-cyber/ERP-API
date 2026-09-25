using BizFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BizFlow.Infrastructure.Data.Configurations;

public class SalesInvoiceConfiguration : IEntityTypeConfiguration<SalesInvoice>
{
    public void Configure(EntityTypeBuilder<SalesInvoice> builder)
    {
        builder.ToTable("SalesInvoices");

        builder.HasKey(inv => inv.Id);

        builder.Property(inv => inv.InvoiceNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(inv => inv.SubTotal)
            .HasColumnType("decimal(18,2)");

        builder.Property(inv => inv.DiscountAmount)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0m);

        builder.Property(inv => inv.TaxAmount)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0m);

        builder.Property(inv => inv.CgstAmount)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0m);

        builder.Property(inv => inv.SgstAmount)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0m);

        builder.Property(inv => inv.IgstAmount)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0m);

        builder.Property(inv => inv.TotalAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(inv => inv.PaidAmount)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0m);

        builder.Property(inv => inv.BalanceAmount)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0m);

        builder.Property(inv => inv.Notes)
            .HasMaxLength(1000);

        builder.HasIndex(inv => new { inv.BusinessId, inv.InvoiceNumber })
            .IsUnique();

        builder.HasOne(inv => inv.Business)
            .WithMany()
            .HasForeignKey(inv => inv.BusinessId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(inv => inv.Customer)
            .WithMany(c => c.Invoices)
            .HasForeignKey(inv => inv.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(inv => inv.SalesOrder)
            .WithMany(o => o.Invoices)
            .HasForeignKey(inv => inv.SalesOrderId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(inv => inv.Warehouse)
            .WithMany()
            .HasForeignKey(inv => inv.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(inv => inv.Items)
            .WithOne(i => i.SalesInvoice)
            .HasForeignKey(i => i.SalesInvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(inv => inv.Payments)
            .WithOne(p => p.SalesInvoice)
            .HasForeignKey(p => p.SalesInvoiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
