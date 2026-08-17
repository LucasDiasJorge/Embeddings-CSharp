using Inventory.Domain.Counting;
using Inventory.Domain.Items;
using Inventory.Domain.Locations;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistence;

public sealed class InventoryDbContext(DbContextOptions<InventoryDbContext> options) : DbContext(options)
{
    public DbSet<Item> Items => Set<Item>();

    public DbSet<Location> Locations => Set<Location>();

    public DbSet<InventoryCount> InventoryCounts => Set<InventoryCount>();

    public DbSet<ItemLedgerEntry> LedgerEntries => Set<ItemLedgerEntry>();

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        builder.Properties<ItemId>().HaveConversion<ItemIdConverter>();
        builder.Properties<LocationId>().HaveConversion<LocationIdConverter>();
        builder.Properties<InventoryCountId>().HaveConversion<InventoryCountIdConverter>();
        builder.Properties<Sku>().HaveConversion<SkuConverter>().HaveMaxLength(Sku.MaxLength);
        builder.Properties<LocationCode>().HaveConversion<LocationCodeConverter>().HaveMaxLength(LocationCode.MaxLength);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InventoryDbContext).Assembly);
}
