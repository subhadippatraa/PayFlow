using PayFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PayFlow.Infrastructure.Persistence.Configurations;

public class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable("IdempotencyRecords");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.IdempotencyKey)
            .IsRequired()
            .HasMaxLength(255);

        builder.HasIndex(r => r.IdempotencyKey)
            .IsUnique()
            .HasDatabaseName("IX_Idempotency_Key");

        builder.Property(r => r.RequestPath)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(r => r.RequestHash)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(r => r.StatusCode).IsRequired();

        builder.Property(r => r.ResponseBody).IsRequired();

        builder.Property(r => r.CreatedAt)
            .IsRequired()
            .HasColumnType("datetime2(3)");

        builder.Property(r => r.ExpiresAt)
            .IsRequired()
            .HasColumnType("datetime2(3)");
    }
}
