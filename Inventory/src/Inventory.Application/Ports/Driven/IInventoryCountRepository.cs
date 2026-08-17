using Inventory.Domain.Counting;
using Inventory.Domain.Locations;

namespace Inventory.Application.Ports.Driven;

public interface IInventoryCountRepository
{
    Task<InventoryCount?> FindByIdAsync(InventoryCountId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Duas contagens abertas na mesma localização produziriam vereditos contraditórios,
    /// então o caso de uso usa isto para recusar a segunda.
    /// </summary>
    Task<InventoryCount?> FindOpenByLocationAsync(
        LocationId locationId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InventoryCount>> ListAsync(
        LocationId? locationId, InventoryCountStatus? status, CancellationToken cancellationToken = default);

    Task AddAsync(InventoryCount count, CancellationToken cancellationToken = default);
}
