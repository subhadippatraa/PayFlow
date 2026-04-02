using PayFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PayFlow.Infrastructure.Persistence.Configurations;

public class LedgerEntryConfiguration : IEntityTypeConfiguration<LedgerEntry>
{
    public void Configure(EntityTypeBuilder<LedgerEntry> builder)
    {
        builder.ToTable("LedgerEntries");

        builder.HasKey(le => le.Id);

        builder.Property(le => le.TransactionId).IsRequired();

        builder.Property(le => le.WalletId).IsRequired();

        builder.Property(le => le.EntryType)
            .IsRequired()
            .HasMaxLength(10)
            .HasConversion<string>();

        builder.Property(le => le.Amount).IsRequired();

        builder.Property(le => le.Currency)
            .IsRequired()
            .HasMaxLength(3)
            .IsFixedLength();

        builder.Property(le => le.RunningBalance).IsRequired();

        builder.Property(le => le.CreatedAt)
            .IsRequired()
            .HasColumnType("datetime2(3)");

        builder.HasIndex(le => le.TransactionId)
            .HasDatabaseName("IX_Ledger_TransactionId");

        builder.HasIndex(le => new { le.WalletId, le.CreatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("IX_Ledger_WalletId");
    }
}
