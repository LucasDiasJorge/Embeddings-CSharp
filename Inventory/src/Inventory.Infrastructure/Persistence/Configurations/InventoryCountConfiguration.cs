using Inventory.Domain.Counting;
using Inventory.Domain.Locations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Configurations;

internal sealed class InventoryCountConfiguration : IEntityTypeConfiguration<InventoryCount>
{
    public void Configure(EntityTypeBuilder<InventoryCount> builder)
    {
        builder.ToTable("inventory_counts");
        builder.HasKey(count => count.Id);

        builder.Property(count => count.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(count => count.LocationId).HasColumnName("location_id");
        builder.Property(count => count.Auditor).HasColumnName("auditor")
            .HasMaxLength(InventoryCount.MaxAuditorLength).IsRequired();
        builder.Property(count => count.Status).HasColumnName("status").HasConversion<int>();
        builder.Property(count => count.OpenedAt).HasColumnName("opened_at");
        builder.Property(count => count.ClosedAt).HasColumnName("closed_at");
        builder.Property(count => count.CancellationReason).HasColumnName("cancellation_reason");

        // O serviço já recusa abrir uma segunda contagem no mesmo lugar, mas essa checagem
        // é vulnerável a duas requisições simultâneas. O índice parcial fecha a corrida no banco.
        builder.HasIndex(count => count.LocationId)
            .IsUnique()
            .HasFilter($"status = {(int)InventoryCountStatus.Open}")
            .HasDatabaseName("ux_inventory_counts_open_per_location");

        builder.HasOne<Location>()
            .WithMany()
            .HasForeignKey(count => count.LocationId)
            .OnDelete(DeleteBehavior.Restrict);

        ConfigureExpected(builder);
        ConfigureScans(builder);

        builder.Ignore(count => count.IsOpen);
    }

    private static void ConfigureExpected(EntityTypeBuilder<InventoryCount> builder)
    {
        builder.OwnsMany(count => count.Expected, expected =>
        {
            expected.ToTable("inventory_count_expected_items");
            expected.WithOwner().HasForeignKey("inventory_count_id");
            expected.Property(item => item.ItemId).HasColumnName("item_id");

            // A chave composta é a própria invariante: um item aparece no máximo
            // uma vez na fotografia de abertura.
            expected.HasKey("inventory_count_id", "ItemId");
        });

        builder.Navigation(count => count.Expected).UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    private static void ConfigureScans(EntityTypeBuilder<InventoryCount> builder)
    {
        builder.OwnsMany(count => count.Scans, scans =>
        {
            scans.ToTable("inventory_count_scans");
            scans.WithOwner().HasForeignKey("inventory_count_id");
            scans.Property(scan => scan.ItemId).HasColumnName("item_id");
            scans.Property(scan => scan.ScannedAt).HasColumnName("scanned_at");

            // Idem: bipar duas vezes não gera duas linhas, no domínio nem no banco.
            scans.HasKey("inventory_count_id", "ItemId");
        });

        builder.Navigation(count => count.Scans).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
