using Inventory.Domain.Items;
using Inventory.Domain.Locations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Configurations;

internal sealed class ItemConfiguration : IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> builder)
    {
        builder.ToTable("items");
        builder.HasKey(item => item.Id);

        builder.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(item => item.Sku).HasColumnName("sku").IsRequired();
        builder.Property(item => item.Name).HasColumnName("name")
            .HasMaxLength(Item.MaxNameLength).IsRequired();
        builder.Property(item => item.Description).HasColumnName("description");
        builder.Property(item => item.LocationId).HasColumnName("location_id");
        builder.Property(item => item.Status).HasColumnName("status").HasConversion<int>();
        builder.Property(item => item.RegisteredAt).HasColumnName("registered_at");
        builder.Property(item => item.LastSeenAt).HasColumnName("last_seen_at");
        builder.Property(item => item.LedgerSequence).HasColumnName("ledger_sequence");

        builder.HasIndex(item => item.Sku).IsUnique().HasDatabaseName("ux_items_sku");
        builder.HasIndex(item => item.LocationId).HasDatabaseName("ix_items_location");

        builder.HasOne<Location>()
            .WithMany()
            .HasForeignKey(item => item.LocationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Caixa de saída em memória e propriedade derivada: existem para o domínio, não para o banco.
        builder.Ignore(item => item.PendingLedgerEntries);
        builder.Ignore(item => item.IsCountable);
    }
}
