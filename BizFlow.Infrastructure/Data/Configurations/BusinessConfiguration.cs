using BizFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BizFlow.Infrastructure.Data.Configurations;

public class BusinessConfiguration : IEntityTypeConfiguration<Business>
{
    public void Configure(EntityTypeBuilder<Business> builder)
    {
        builder.ToTable("Businesses");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.BusinessCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(b => b.BusinessCode)
            .IsUnique();

        builder.Property(b => b.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(b => b.LegalName)
            .HasMaxLength(250);

        builder.Property(b => b.GSTNumber)
            .HasMaxLength(50);

        builder.Property(b => b.Email)
            .HasMaxLength(150);

        builder.Property(b => b.Phone)
            .HasMaxLength(30);

        builder.Property(b => b.Address)
            .HasMaxLength(500);

        builder.Property(b => b.Currency)
            .IsRequired()
            .HasMaxLength(10)
            .HasDefaultValue("INR");

        builder.Property(b => b.IsActive)
            .HasDefaultValue(true);

        builder.Property(b => b.CreatedBy)
            .HasMaxLength(150);

        builder.Property(b => b.UpdatedBy)
            .HasMaxLength(150);
    }
}
