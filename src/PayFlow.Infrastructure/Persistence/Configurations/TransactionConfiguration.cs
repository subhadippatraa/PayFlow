using PayFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PayFlow.Infrastructure.Persistence.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transactions");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.IdempotencyKey)
            .IsRequired()
            .HasMaxLength(255);

        builder.HasIndex(t => t.IdempotencyKey)
            .IsUnique()
            .HasDatabaseName("IX_Transactions_IdempotencyKey");

        builder.Property(t => t.Type)
            .IsRequired()
            .HasMaxLength(50)
            .HasConversion<string>();

        builder.Property(t => t.Status)
            .IsRequired()
            .HasMaxLength(50)
            .HasConversion<string>();

        builder.Property(t => t.SourceWalletId).IsRequired();

        builder.Property(t => t.Amount).IsRequired();

        builder.Property(t => t.Currency)
            .IsRequired()
            .HasMaxLength(3)
            .IsFixedLength();

        builder.Property(t => t.Description).HasMaxLength(500);

        builder.Property(t => t.FailureReason).HasMaxLength(1000);

        builder.Property(t => t.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.Property(t => t.CreatedAt)
            .IsRequired()
            .HasColumnType("datetime2(3)");

        builder.Property(t => t.CompletedAt)
            .HasColumnType("datetime2(3)");

        // Indexes for common queries
        builder.HasIndex(t => new { t.SourceWalletId, t.CreatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("IX_Transactions_SourceWallet");

        builder.HasIndex(t => new { t.DestinationWalletId, t.CreatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("IX_Transactions_DestWallet")
            .HasFilter("[DestinationWalletId] IS NOT NULL");

        builder.HasIndex(t => t.Status)
            .HasDatabaseName("IX_Transactions_Status")
            .HasFilter("[Status] IN ('Pending', 'Processing')");

        // Relationships
        builder.HasOne(t => t.SourceWallet)
            .WithMany()
            .HasForeignKey(t => t.SourceWalletId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.DestinationWallet)
            .WithMany()
            .HasForeignKey(t => t.DestinationWalletId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.OriginalTransaction)
            .WithMany()
            .HasForeignKey(t => t.OriginalTransactionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(t => t.LedgerEntries)
            .WithOne(le => le.Transaction)
            .HasForeignKey(le => le.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
