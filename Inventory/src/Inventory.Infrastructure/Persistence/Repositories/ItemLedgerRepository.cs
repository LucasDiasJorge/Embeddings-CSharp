using Inventory.Application.Ports.Driven;
using Inventory.Domain.Counting;
using Inventory.Domain.Items;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistence.Repositories;

internal sealed class ItemLedgerRepository(InventoryDbContext context) : IItemLedgerRepository
{
    public async Task AppendAsync(
        IReadOnlyCollection<ItemLedgerEntry> entries, CancellationToken cancellationToken = default) =>
        await context.LedgerEntries.AddRangeAsync(entries, cancellationToken);

    // Leitura sempre sem rastreamento: o razão é imutável, rastrear seria só custo.
    public async Task<IReadOnlyList<ItemLedgerEntry>> ListByItemAsync(
        ItemId itemId, CancellationToken cancellationToken = default) =>
        await context.LedgerEntries
            .AsNoTracking()
            .Where(entry => entry.ItemId == itemId)
            .OrderBy(entry => entry.Sequence)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ItemLedgerEntry>> ListPageAsync(
        int skip, int take, CancellationToken cancellationToken = default) =>
        await context.LedgerEntries
            .AsNoTracking()
            .OrderBy(entry => entry.OccurredAt)
            .ThenBy(entry => entry.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ItemLedgerEntry>> ListByCountAsync(
        InventoryCountId inventoryCountId, CancellationToken cancellationToken = default) =>
        await context.LedgerEntries
            .AsNoTracking()
            .Where(entry => entry.InventoryCountId == inventoryCountId)
            .OrderBy(entry => entry.OccurredAt)
            .ThenBy(entry => entry.Sequence)
            .ToListAsync(cancellationToken);
}
