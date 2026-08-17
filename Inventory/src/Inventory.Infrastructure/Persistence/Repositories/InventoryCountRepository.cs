using Inventory.Application.Ports.Driven;
using Inventory.Domain.Counting;
using Inventory.Domain.Locations;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistence.Repositories;

internal sealed class InventoryCountRepository(InventoryDbContext context) : IInventoryCountRepository
{
    // Esperados e leituras são tipos owned: o EF sempre os carrega junto do agregado,
    // não há Include a esquecer.
    public Task<InventoryCount?> FindByIdAsync(
        InventoryCountId id, CancellationToken cancellationToken = default) =>
        context.InventoryCounts.FirstOrDefaultAsync(count => count.Id == id, cancellationToken);

    public Task<InventoryCount?> FindOpenByLocationAsync(
        LocationId locationId, CancellationToken cancellationToken = default) =>
        context.InventoryCounts.FirstOrDefaultAsync(
            count => count.LocationId == locationId && count.Status == InventoryCountStatus.Open,
            cancellationToken);

    public async Task<IReadOnlyList<InventoryCount>> ListAsync(
        LocationId? locationId, InventoryCountStatus? status, CancellationToken cancellationToken = default)
    {
        var counts = context.InventoryCounts.AsNoTracking();

        if (locationId is { } location)
        {
            counts = counts.Where(count => count.LocationId == location);
        }

        if (status is { } wanted)
        {
            counts = counts.Where(count => count.Status == wanted);
        }

        return await counts
            .OrderByDescending(count => count.OpenedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(InventoryCount count, CancellationToken cancellationToken = default) =>
        await context.InventoryCounts.AddAsync(count, cancellationToken);
}
