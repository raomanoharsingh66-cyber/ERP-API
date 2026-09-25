using BizFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BizFlow.Infrastructure.Data.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.CustomerCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Email)
            .HasMaxLength(150);

        builder.Property(c => c.Phone)
            .HasMaxLength(50);

        builder.Property(c => c.GSTIN)
            .HasMaxLength(20);

        builder.Property(c => c.PAN)
            .HasMaxLength(20);

        builder.Property(c => c.BillingAddress).HasMaxLength(300);
        builder.Property(c => c.BillingCity).HasMaxLength(100);
        builder.Property(c => c.BillingState).HasMaxLength(100);
        builder.Property(c => c.BillingPostalCode).HasMaxLength(20);
        builder.Property(c => c.BillingCountry).HasMaxLength(100);

        builder.Property(c => c.ShippingAddress).HasMaxLength(300);
        builder.Property(c => c.ShippingCity).HasMaxLength(100);
        builder.Property(c => c.ShippingState).HasMaxLength(100);
        builder.Property(c => c.ShippingPostalCode).HasMaxLength(20);
        builder.Property(c => c.ShippingCountry).HasMaxLength(100);

        builder.Property(c => c.CreditLimit)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0m);

        builder.Property(c => c.OutstandingBalance)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0m);

        builder.Property(c => c.IsActive)
            .HasDefaultValue(true);

        builder.Property(c => c.Notes)
            .HasMaxLength(1000);

        builder.HasIndex(c => new { c.BusinessId, c.CustomerCode })
            .IsUnique();

        builder.HasIndex(c => new { c.BusinessId, c.Name });

        builder.HasOne(c => c.Business)
            .WithMany()
            .HasForeignKey(c => c.BusinessId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
