using BizFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BizFlow.Infrastructure.Data.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.SKU)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.Barcode)
            .HasMaxLength(50);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Description)
            .HasMaxLength(1000);

        builder.Property(p => p.PurchasePrice)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0m);

        builder.Property(p => p.SellingPrice)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0m);

        builder.Property(p => p.TaxRate)
            .HasColumnType("decimal(5,2)")
            .HasDefaultValue(18.0m);

        builder.Property(p => p.HSNCode)
            .HasMaxLength(20);

        builder.Property(p => p.MinStockLevel)
            .HasColumnType("decimal(18,3)")
            .HasDefaultValue(10m);

        builder.Property(p => p.MaxStockLevel)
            .HasColumnType("decimal(18,3)")
            .HasDefaultValue(1000m);

        builder.Property(p => p.IsActive)
            .HasDefaultValue(true);

        builder.HasIndex(p => new { p.BusinessId, p.SKU })
            .IsUnique();

        builder.HasIndex(p => new { p.BusinessId, p.Barcode });

        builder.HasOne(p => p.Business)
            .WithMany()
            .HasForeignKey(p => p.BusinessId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.UnitOfMeasure)
            .WithMany(u => u.Products)
            .HasForeignKey(p => p.UnitOfMeasureId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
