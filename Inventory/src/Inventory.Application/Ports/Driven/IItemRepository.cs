using Inventory.Application.Contracts.Items;
using Inventory.Domain.Items;
using Inventory.Domain.Locations;

namespace Inventory.Application.Ports.Driven;

public interface IItemRepository
{
    Task<Item?> FindByIdAsync(ItemId id, CancellationToken cancellationToken = default);

    Task<Item?> FindBySkuAsync(Sku sku, CancellationToken cancellationToken = default);

    /// <summary>Carrega vários itens de uma vez — usado no fechamento da contagem.</summary>
    Task<IReadOnlyList<Item>> FindManyAsync(IEnumerable<ItemId> ids, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Item>> SearchAsync(ItemQuery query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Os itens que o sistema acredita estarem numa localização. É esta consulta que
    /// vira a fotografia congelada na abertura de uma contagem.
    /// </summary>
    Task<IReadOnlyList<Item>> ListCountableByLocationAsync(
        LocationId locationId, CancellationToken cancellationToken = default);

    Task<bool> ExistsWithSkuAsync(Sku sku, CancellationToken cancellationToken = default);

    Task<int> CountByLocationAsync(LocationId locationId, CancellationToken cancellationToken = default);

    Task AddAsync(Item item, CancellationToken cancellationToken = default);
}
