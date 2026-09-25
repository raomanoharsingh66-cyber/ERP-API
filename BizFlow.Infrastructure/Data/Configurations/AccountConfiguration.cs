using BizFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BizFlow.Infrastructure.Data.Configurations;

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("Accounts");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.AccountCode)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(a => a.AccountName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(a => a.Type)
            .IsRequired();

        builder.Property(a => a.Subtype)
            .HasMaxLength(100);

        builder.Property(a => a.Description)
            .HasMaxLength(500);

        builder.Property(a => a.CurrentBalance)
            .HasPrecision(18, 2)
            .HasDefaultValue(0);

        builder.Property(a => a.IsSystemAccount)
            .HasDefaultValue(false);

        builder.Property(a => a.IsActive)
            .HasDefaultValue(true);

        builder.HasIndex(a => new { a.BusinessId, a.AccountCode })
            .IsUnique();

        builder.HasOne(a => a.Business)
            .WithMany()
            .HasForeignKey(a => a.BusinessId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(a => a.JournalLines)
            .WithOne(l => l.Account)
            .HasForeignKey(l => l.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
