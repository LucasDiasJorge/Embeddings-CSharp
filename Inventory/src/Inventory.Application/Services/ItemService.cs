using Inventory.Application.Common;
using Inventory.Application.Contracts.Items;
using Inventory.Application.Narration;
using Inventory.Application.Ports.Driven;
using Inventory.Domain.Items;
using Inventory.Domain.Locations;

namespace Inventory.Application.Services;

public sealed class ItemService(
    IItemRepository items,
    ILocationRepository locations,
    IItemLedgerRepository ledger,
    LedgerCommitter committer,
    ItemNarrator narrator,
    IClock clock)
{
    private const int MaxPageSize = 200;

    public async Task<Result<ItemDto>> RegisterAsync(
        RegisterItemCommand command, CancellationToken cancellationToken = default)
    {
        var sku = Sku.Create(command.Sku);

        if (await items.ExistsWithSkuAsync(sku, cancellationToken))
        {
            return Error.Conflict("item.duplicate_sku", $"Já existe um item com a etiqueta {sku}.");
        }

        var locationId = new LocationId(command.LocationId);
        var location = await locations.FindByIdAsync(locationId, cancellationToken);

        if (location is null)
        {
            return LocationNotFound(locationId);
        }

        if (!location.IsActive)
        {
            return LocationInactive(location);
        }

        var item = Item.Register(sku, command.Name, command.Description, locationId, command.Actor, clock.UtcNow);

        await items.AddAsync(item, cancellationToken);
        await committer.CommitAsync([item], cancellationToken);

        return ItemDto.From(item, location);
    }

    public async Task<Result<ItemDto>> MoveAsync(
        ItemId id, MoveItemCommand command, CancellationToken cancellationToken = default)
    {
        if (await items.FindByIdAsync(id, cancellationToken) is not { } item)
        {
            return ItemNotFound(id);
        }

        var destinationId = new LocationId(command.DestinationLocationId);
        var destination = await locations.FindByIdAsync(destinationId, cancellationToken);

        if (destination is null)
        {
            return LocationNotFound(destinationId);
        }

        if (!destination.IsActive)
        {
            return LocationInactive(destination);
        }

        // Movimentar sem dizer o motivo é o caso comum: transferência operacional.
        var reason = command.Reason ?? MovementReason.Transfer;

        item.MoveTo(destinationId, reason, command.Actor, command.Note, clock.UtcNow);
        await committer.CommitAsync([item], cancellationToken);

        return ItemDto.From(item, destination);
    }

    public async Task<Result<ItemDto>> RenameAsync(
        ItemId id, RenameItemCommand command, CancellationToken cancellationToken = default)
    {
        if (await items.FindByIdAsync(id, cancellationToken) is not { } item)
        {
            return ItemNotFound(id);
        }

        item.Rename(command.Name, command.Description, command.Actor, clock.UtcNow);
        await committer.CommitAsync([item], cancellationToken);

        return await DescribeAsync(item, cancellationToken);
    }

    public async Task<Result<ItemDto>> RetireAsync(
        ItemId id, RetireItemCommand command, CancellationToken cancellationToken = default)
    {
        if (await items.FindByIdAsync(id, cancellationToken) is not { } item)
        {
            return ItemNotFound(id);
        }

        item.Retire(command.Actor, command.Note, clock.UtcNow);
        await committer.CommitAsync([item], cancellationToken);

        return await DescribeAsync(item, cancellationToken);
    }

    public async Task<Result<ItemDto>> GetAsync(ItemId id, CancellationToken cancellationToken = default) =>
        await items.FindByIdAsync(id, cancellationToken) is { } item
            ? await DescribeAsync(item, cancellationToken)
            : Result<ItemDto>.Failure(ItemNotFound(id));

    public async Task<Result<ItemDto>> GetBySkuAsync(string sku, CancellationToken cancellationToken = default)
    {
        var parsed = Sku.Create(sku);

        return await items.FindBySkuAsync(parsed, cancellationToken) is { } item
            ? await DescribeAsync(item, cancellationToken)
            : Result<ItemDto>.Failure(Error.NotFound("item.not_found", $"Não existe item com a etiqueta {parsed}."));
    }

    public async Task<Result<IReadOnlyList<ItemDto>>> SearchAsync(
        ItemQuery query, CancellationToken cancellationToken = default)
    {
        var safe = query with { Take = Math.Clamp(query.Take, 1, MaxPageSize), Skip = Math.Max(query.Skip, 0) };
        var found = await items.SearchAsync(safe, cancellationToken);
        var byId = await LoadLocationsAsync(found, cancellationToken);

        return Result<IReadOnlyList<ItemDto>>.Success(
            [.. found.Select(item => ItemDto.From(item, byId.GetValueOrDefault(item.LocationId)))]);
    }

    public async Task<Result<ItemHistoryDto>> GetHistoryAsync(
        ItemId id, CancellationToken cancellationToken = default)
    {
        if (await items.FindByIdAsync(id, cancellationToken) is not { } item)
        {
            return Error.NotFound("item.not_found", $"O item {id} não existe.");
        }

        var entries = await ledger.ListByItemAsync(id, cancellationToken);

        // Uma única consulta resolve todas as localizações citadas na linha do tempo,
        // incluindo a atual do item (que pode não aparecer em nenhum lançamento antigo).
        var referenced = entries
            .SelectMany(entry => new[] { entry.FromLocationId, entry.ToLocationId })
            .OfType<LocationId>()
            .Append(item.LocationId)
            .Distinct();

        var byId = await locations.FindManyAsync(referenced, cancellationToken);

        var timeline = entries
            .OrderBy(entry => entry.Sequence)
            .Select(entry => new ItemHistoryEntryDto(
                entry.Id,
                entry.Sequence,
                entry.Kind.ToString(),
                entry.OccurredAt,
                entry.Actor,
                Describe(entry.FromLocationId, byId),
                Describe(entry.ToLocationId, byId),
                entry.Reason?.ToString(),
                entry.InventoryCountId?.Value,
                entry.Note,
                narrator.Narrate(entry, item, byId)))
            .ToArray();

        return new ItemHistoryDto(ItemDto.From(item, byId.GetValueOrDefault(item.LocationId)), timeline);
    }

    private async Task<ItemDto> DescribeAsync(Item item, CancellationToken cancellationToken) =>
        ItemDto.From(item, await locations.FindByIdAsync(item.LocationId, cancellationToken));

    private async Task<IReadOnlyDictionary<LocationId, Location>> LoadLocationsAsync(
        IEnumerable<Item> found, CancellationToken cancellationToken) =>
        await locations.FindManyAsync(found.Select(item => item.LocationId).Distinct(), cancellationToken);

    private static string? Describe(LocationId? id, IReadOnlyDictionary<LocationId, Location> byId) =>
        id is { } locationId && byId.TryGetValue(locationId, out var location)
            ? $"{location.Name} ({location.Code.Value})"
            : null;

    private static Error ItemNotFound(ItemId id) =>
        Error.NotFound("item.not_found", $"O item {id} não existe.");

    private static Error LocationNotFound(LocationId id) =>
        Error.NotFound("location.not_found", $"A localização {id} não existe.");

    private static Error LocationInactive(Location location) =>
        Error.Conflict("location.inactive", $"A localização {location.Code} está inativa e não recebe itens.");
}
