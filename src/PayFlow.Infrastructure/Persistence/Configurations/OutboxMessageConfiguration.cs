using PayFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PayFlow.Infrastructure.Persistence.Configurations;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .UseIdentityColumn();

        builder.Property(m => m.EventType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(m => m.Payload)
            .IsRequired();

        builder.Property(m => m.CorrelationId)
            .HasMaxLength(100);

        builder.Property(m => m.PublishedAt)
            .HasColumnType("datetime2(3)");

        builder.Property(m => m.CreatedAt)
            .IsRequired()
            .HasColumnType("datetime2(3)");

        // Index for polling unpublished messages
        builder.HasIndex(m => m.PublishedAt)
            .HasDatabaseName("IX_Outbox_PublishedAt")
            .HasFilter("[PublishedAt] IS NULL");
    }
}
