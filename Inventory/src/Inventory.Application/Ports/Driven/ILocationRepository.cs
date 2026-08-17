using Inventory.Domain.Locations;

namespace Inventory.Application.Ports.Driven;

public interface ILocationRepository
{
    Task<Location?> FindByIdAsync(LocationId id, CancellationToken cancellationToken = default);

    Task<Location?> FindByCodeAsync(LocationCode code, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Location>> ListAsync(bool includeInactive, CancellationToken cancellationToken = default);

    /// <summary>
    /// Carrega várias localizações de uma vez. Existe para o narrador resolver os nomes
    /// de um histórico inteiro sem cair em N+1.
    /// </summary>
    Task<IReadOnlyDictionary<LocationId, Location>> FindManyAsync(
        IEnumerable<LocationId> ids, CancellationToken cancellationToken = default);

    Task AddAsync(Location location, CancellationToken cancellationToken = default);
}
