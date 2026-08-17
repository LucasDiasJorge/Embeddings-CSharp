using Inventory.Domain.Items;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Configurations;

internal sealed class ItemLedgerEntryConfiguration : IEntityTypeConfiguration<ItemLedgerEntry>
{
    public void Configure(EntityTypeBuilder<ItemLedgerEntry> builder)
    {
        builder.ToTable("item_ledger_entries");
        builder.HasKey(entry => entry.Id);

        builder.Property(entry => entry.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(entry => entry.ItemId).HasColumnName("item_id");
        builder.Property(entry => entry.Sequence).HasColumnName("sequence");
        builder.Property(entry => entry.Kind).HasColumnName("kind").HasConversion<int>();
        builder.Property(entry => entry.OccurredAt).HasColumnName("occurred_at");
        builder.Property(entry => entry.Actor).HasColumnName("actor").HasMaxLength(160).IsRequired();
        builder.Property(entry => entry.FromLocationId).HasColumnName("from_location_id");
        builder.Property(entry => entry.ToLocationId).HasColumnName("to_location_id");
        builder.Property(entry => entry.Reason).HasColumnName("reason");
        builder.Property(entry => entry.InventoryCountId).HasColumnName("inventory_count_id");
        builder.Property(entry => entry.Note).HasColumnName("note");

        // A unicidade de (item, sequência) é o que garante, no nível do banco, que o razão
        // não ganhe duas linhas na mesma posição nem perca a ordem sob concorrência.
        builder.HasIndex(entry => new { entry.ItemId, entry.Sequence })
            .IsUnique()
            .HasDatabaseName("ux_ledger_item_sequence");

        builder.HasIndex(entry => entry.InventoryCountId).HasDatabaseName("ix_ledger_count");

        builder.HasOne<Item>()
            .WithMany()
            .HasForeignKey(entry => entry.ItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
