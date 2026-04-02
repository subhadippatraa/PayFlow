using PayFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PayFlow.Infrastructure.Persistence.Configurations;

public class WalletConfiguration : IEntityTypeConfiguration<Wallet>
{
    public void Configure(EntityTypeBuilder<Wallet> builder)
    {
        builder.ToTable("Wallets");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.UserId).IsRequired();

        builder.Property(w => w.Currency)
            .IsRequired()
            .HasMaxLength(3)
            .IsFixedLength();

        builder.Property(w => w.Balance)
            .IsRequired()
            .HasDefaultValue(0L);

        builder.Property(w => w.HeldBalance)
            .IsRequired()
            .HasDefaultValue(0L);

        builder.Property(w => w.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Optimistic concurrency token — maps to SQL Server ROWVERSION
        builder.Property(w => w.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.Property(w => w.CreatedAt)
            .IsRequired()
            .HasColumnType("datetime2(3)");

        builder.Property(w => w.UpdatedAt)
            .IsRequired()
            .HasColumnType("datetime2(3)");

        // Unique constraint: one wallet per user per currency
        builder.HasIndex(w => new { w.UserId, w.Currency })
            .IsUnique()
            .HasDatabaseName("UQ_Wallet_User_Currency");

        builder.HasIndex(w => w.UserId)
            .HasDatabaseName("IX_Wallets_UserId");

        builder.HasMany(w => w.LedgerEntries)
            .WithOne(le => le.Wallet)
            .HasForeignKey(le => le.WalletId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
