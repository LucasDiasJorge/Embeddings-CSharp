using Inventory.Application.Contracts.Insights;
using Inventory.Application.Contracts.Items;
using Inventory.Application.Ports.Driven;
using Inventory.Domain.Counting;
using Inventory.Domain.Items;
using Inventory.Domain.Locations;

namespace Inventory.Tests.Fakes;

/// <summary>
/// Adaptadores dirigidos em memória.
/// </summary>
/// <remarks>
/// Estes fakes são o argumento prático da arquitetura hexagonal: eles somam pouco mais
/// de cem linhas, não precisam de Postgres, de Docker nem de Ollama, e mesmo assim
/// exercitam os casos de uso <b>reais</b> — nenhuma regra é reimplementada aqui.
/// Se um dia isso deixar de ser possível, é sinal de que regra de negócio vazou para
/// dentro de um repositório.
/// </remarks>
internal sealed class FakeClock(DateTimeOffset now) : IClock
{
    public DateTimeOffset UtcNow { get; set; } = now;

    public void Advance(TimeSpan by) => UtcNow += by;
}

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCount++;
        return Task.FromResult(0);
    }
}

internal sealed class FakeLocationRepository : ILocationRepository
{
    private readonly List<Location> _locations = [];

    public Task<Location?> FindByIdAsync(LocationId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_locations.FirstOrDefault(location => location.Id == id));

    public Task<Location?> FindByCodeAsync(LocationCode code, CancellationToken cancellationToken = default) =>
        Task.FromResult(_locations.FirstOrDefault(location => location.Code == code));

    public Task<IReadOnlyList<Location>> ListAsync(
        bool includeInactive, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Location>>(
            [.. _locations.Where(location => includeInactive || location.IsActive)]);

    public Task<IReadOnlyDictionary<LocationId, Location>> FindManyAsync(
        IEnumerable<LocationId> ids, CancellationToken cancellationToken = default)
    {
        var wanted = ids.ToHashSet();

        return Task.FromResult<IReadOnlyDictionary<LocationId, Location>>(
            _locations.Where(location => wanted.Contains(location.Id)).ToDictionary(location => location.Id));
    }

    public Task AddAsync(Location location, CancellationToken cancellationToken = default)
    {
        _locations.Add(location);
        return Task.CompletedTask;
    }
}

internal sealed class FakeItemRepository : IItemRepository
{
    private readonly List<Item> _items = [];

    public Task<Item?> FindByIdAsync(ItemId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.FirstOrDefault(item => item.Id == id));

    public Task<Item?> FindBySkuAsync(Sku sku, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.FirstOrDefault(item => item.Sku == sku));

    public Task<IReadOnlyList<Item>> FindManyAsync(
        IEnumerable<ItemId> ids, CancellationToken cancellationToken = default)
    {
        var wanted = ids.ToHashSet();

        return Task.FromResult<IReadOnlyList<Item>>([.. _items.Where(item => wanted.Contains(item.Id))]);
    }

    public Task<IReadOnlyList<Item>> SearchAsync(ItemQuery query, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Item>>(
        [
            .. _items
                .Where(item => query.LocationId is not { } location || item.LocationId == location)
                .Where(item => query.Status is not { } status || item.Status == status)
                .Skip(query.Skip)
                .Take(query.Take)
        ]);

    public Task<IReadOnlyList<Item>> ListCountableByLocationAsync(
        LocationId locationId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Item>>(
            [.. _items.Where(item => item.LocationId == locationId && item.IsCountable)]);

    public Task<bool> ExistsWithSkuAsync(Sku sku, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.Any(item => item.Sku == sku));

    public Task<int> CountByLocationAsync(LocationId locationId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.Count(item => item.LocationId == locationId && item.IsCountable));

    public Task AddAsync(Item item, CancellationToken cancellationToken = default)
    {
        _items.Add(item);
        return Task.CompletedTask;
    }
}

internal sealed class FakeInventoryCountRepository : IInventoryCountRepository
{
    private readonly List<InventoryCount> _counts = [];

    public Task<InventoryCount?> FindByIdAsync(
        InventoryCountId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_counts.FirstOrDefault(count => count.Id == id));

    public Task<InventoryCount?> FindOpenByLocationAsync(
        LocationId locationId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_counts.FirstOrDefault(count => count.LocationId == locationId && count.IsOpen));

    public Task<IReadOnlyList<InventoryCount>> ListAsync(
        LocationId? locationId, InventoryCountStatus? status, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<InventoryCount>>(
        [
            .. _counts
                .Where(count => locationId is not { } location || count.LocationId == location)
                .Where(count => status is not { } wanted || count.Status == wanted)
        ]);

    public Task AddAsync(InventoryCount count, CancellationToken cancellationToken = default)
    {
        _counts.Add(count);
        return Task.CompletedTask;
    }
}

internal sealed class FakeItemLedgerRepository : IItemLedgerRepository
{
    public List<ItemLedgerEntry> Entries { get; } = [];

    public Task AppendAsync(
        IReadOnlyCollection<ItemLedgerEntry> entries, CancellationToken cancellationToken = default)
    {
        Entries.AddRange(entries);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ItemLedgerEntry>> ListByItemAsync(
        ItemId itemId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ItemLedgerEntry>>(
            [.. Entries.Where(entry => entry.ItemId == itemId).OrderBy(entry => entry.Sequence)]);

    public Task<IReadOnlyList<ItemLedgerEntry>> ListByCountAsync(
        InventoryCountId inventoryCountId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ItemLedgerEntry>>(
            [.. Entries.Where(entry => entry.InventoryCountId == inventoryCountId)]);

    public Task<IReadOnlyList<ItemLedgerEntry>> ListPageAsync(
        int skip, int take, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ItemLedgerEntry>>(
            [.. Entries.OrderBy(entry => entry.OccurredAt).ThenBy(entry => entry.Id).Skip(skip).Take(take)]);
}

/// <summary>
/// Índice semântico de mentira: guarda as narrativas e "busca" por substring.
/// Não avalia qualidade de embedding — avalia que a aplicação narra e indexa
/// os eventos certos, que é a parte que o Ollama não vai consertar se estiver errada.
/// </summary>
internal sealed class FakeNarrativeIndex(bool enabled = true) : IItemNarrativeIndex
{
    public List<ItemNarrative> Indexed { get; } = [];

    public bool IsEnabled { get; } = enabled;

    public Task IndexAsync(
        IReadOnlyCollection<ItemNarrative> narratives, CancellationToken cancellationToken = default)
    {
        Indexed.RemoveAll(existing => narratives.Any(fresh => fresh.LedgerEntryId == existing.LedgerEntryId));
        Indexed.AddRange(narratives);

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<NarrativeMatch>> SearchAsync(
        string query, ItemId? scope, int top, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<NarrativeMatch>>(
        [
            .. Indexed
                .Where(narrative => scope is not { } item || narrative.ItemId == item)
                .Where(narrative => narrative.Text.Contains(query, StringComparison.OrdinalIgnoreCase))
                .Take(top)
                .Select(narrative => new NarrativeMatch(
                    narrative.LedgerEntryId, narrative.ItemId, narrative.Sku,
                    narrative.OccurredAt, narrative.Text, Score: 0.9))
        ]);
}
