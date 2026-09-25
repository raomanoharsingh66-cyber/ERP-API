using BizFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BizFlow.Infrastructure.Data.Configurations;

public class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.ToTable("Warehouses");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(w => w.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(w => w.Address)
            .HasMaxLength(500);

        builder.Property(w => w.ContactPerson)
            .HasMaxLength(100);

        builder.Property(w => w.Phone)
            .HasMaxLength(30);

        builder.Property(w => w.IsDefault)
            .HasDefaultValue(false);

        builder.Property(w => w.IsActive)
            .HasDefaultValue(true);

        builder.HasIndex(w => new { w.BusinessId, w.Code })
            .IsUnique();

        builder.HasOne(w => w.Business)
            .WithMany()
            .HasForeignKey(w => w.BusinessId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
