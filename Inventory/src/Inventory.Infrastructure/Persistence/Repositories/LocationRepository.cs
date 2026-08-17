using Inventory.Application.Ports.Driven;
using Inventory.Domain.Locations;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistence.Repositories;

internal sealed class LocationRepository(InventoryDbContext context) : ILocationRepository
{
    public Task<Location?> FindByIdAsync(LocationId id, CancellationToken cancellationToken = default) =>
        context.Locations.FirstOrDefaultAsync(location => location.Id == id, cancellationToken);

    public Task<Location?> FindByCodeAsync(LocationCode code, CancellationToken cancellationToken = default) =>
        context.Locations.FirstOrDefaultAsync(location => location.Code == code, cancellationToken);

    public async Task<IReadOnlyList<Location>> ListAsync(
        bool includeInactive, CancellationToken cancellationToken = default) =>
        await context.Locations
            .AsNoTracking()
            .Where(location => includeInactive || location.IsActive)
            .OrderBy(location => location.Code)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<LocationId, Location>> FindManyAsync(
        IEnumerable<LocationId> ids, CancellationToken cancellationToken = default)
    {
        var wanted = ids.Distinct().ToArray();

        if (wanted.Length == 0)
        {
            return new Dictionary<LocationId, Location>();
        }

        var found = await context.Locations
            .AsNoTracking()
            .Where(location => wanted.Contains(location.Id))
            .ToListAsync(cancellationToken);

        return found.ToDictionary(location => location.Id);
    }

    public async Task AddAsync(Location location, CancellationToken cancellationToken = default) =>
        await context.Locations.AddAsync(location, cancellationToken);
}
