using BizFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BizFlow.Infrastructure.Data.Configurations;

public class JournalEntryConfiguration : IEntityTypeConfiguration<JournalEntry>
{
    public void Configure(EntityTypeBuilder<JournalEntry> builder)
    {
        builder.ToTable("JournalEntries");

        builder.HasKey(j => j.Id);

        builder.Property(j => j.EntryNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(j => j.Reference)
            .HasMaxLength(100);

        builder.Property(j => j.Narration)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(j => j.TotalDebit)
            .HasPrecision(18, 2);

        builder.Property(j => j.TotalCredit)
            .HasPrecision(18, 2);

        builder.Property(j => j.IsPosted)
            .HasDefaultValue(true);

        builder.HasIndex(j => new { j.BusinessId, j.EntryNumber })
            .IsUnique();

        builder.HasOne(j => j.Business)
            .WithMany()
            .HasForeignKey(j => j.BusinessId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(j => j.Lines)
            .WithOne(l => l.JournalEntry)
            .HasForeignKey(l => l.JournalEntryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
