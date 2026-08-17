using Inventory.Application.Contracts.Items;
using Inventory.Application.Ports.Driven;
using Inventory.Domain.Items;
using Inventory.Domain.Locations;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistence.Repositories;

internal sealed class ItemRepository(InventoryDbContext context) : IItemRepository
{
    public Task<Item?> FindByIdAsync(ItemId id, CancellationToken cancellationToken = default) =>
        context.Items.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

    public Task<Item?> FindBySkuAsync(Sku sku, CancellationToken cancellationToken = default) =>
        context.Items.FirstOrDefaultAsync(item => item.Sku == sku, cancellationToken);

    public async Task<IReadOnlyList<Item>> FindManyAsync(
        IEnumerable<ItemId> ids, CancellationToken cancellationToken = default)
    {
        var wanted = ids.Distinct().ToArray();

        // Rastreados de propósito: quem pede vários itens de uma vez está fechando
        // uma contagem e vai mexer neles.
        return wanted.Length == 0
            ? []
            : await context.Items.Where(item => wanted.Contains(item.Id)).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Item>> SearchAsync(
        ItemQuery query, CancellationToken cancellationToken = default)
    {
        var items = context.Items.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Sku))
        {
            var sku = Sku.Create(query.Sku);
            items = items.Where(item => item.Sku == sku);
        }

        if (query.LocationId is { } locationId)
        {
            items = items.Where(item => item.LocationId == locationId);
        }

        if (query.Status is { } status)
        {
            items = items.Where(item => item.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{query.Search.Trim()}%";
            items = items.Where(item =>
                EF.Functions.ILike(item.Name, pattern) ||
                (item.Description != null && EF.Functions.ILike(item.Description, pattern)));
        }

        return await items
            .OrderBy(item => item.Name)
            .Skip(query.Skip)
            .Take(query.Take)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Item>> ListCountableByLocationAsync(
        LocationId locationId, CancellationToken cancellationToken = default) =>
        await context.Items
            .AsNoTracking()
            .Where(item => item.LocationId == locationId && item.Status != ItemStatus.Retired)
            .OrderBy(item => item.Sku)
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsWithSkuAsync(Sku sku, CancellationToken cancellationToken = default) =>
        context.Items.AnyAsync(item => item.Sku == sku, cancellationToken);

    public Task<int> CountByLocationAsync(LocationId locationId, CancellationToken cancellationToken = default) =>
        context.Items.CountAsync(
            item => item.LocationId == locationId && item.Status != ItemStatus.Retired, cancellationToken);

    public async Task AddAsync(Item item, CancellationToken cancellationToken = default) =>
        await context.Items.AddAsync(item, cancellationToken);
}
