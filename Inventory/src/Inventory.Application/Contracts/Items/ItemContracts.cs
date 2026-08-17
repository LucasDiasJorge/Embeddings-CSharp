using Inventory.Domain.Items;
using Inventory.Domain.Locations;

namespace Inventory.Application.Contracts.Items;

// Os comandos carregam apenas o que vem no corpo da requisição, com Guid cru.
// O identificador do recurso alvo é parâmetro do caso de uso, tipado — assim o
// mesmo record serve de contrato HTTP sem que o adaptador precise remontá-lo.

public sealed record RegisterItemCommand(
    string Sku,
    string Name,
    string? Description,
    Guid LocationId,
    string Actor);

/// <param name="Reason">Ausente, assume <see cref="MovementReason.Transfer"/>.</param>
public sealed record MoveItemCommand(
    Guid DestinationLocationId,
    string Actor,
    MovementReason? Reason,
    string? Note);

public sealed record RenameItemCommand(string Name, string? Description, string Actor);

public sealed record RetireItemCommand(string Actor, string? Note);

/// <summary>Filtros da listagem. <paramref name="Take"/> é limitado pelo serviço para não virar full scan.</summary>
public sealed record ItemQuery(
    string? Sku = null,
    LocationId? LocationId = null,
    ItemStatus? Status = null,
    string? Search = null,
    int Skip = 0,
    int Take = 50);

public sealed record ItemDto(
    Guid Id,
    string Sku,
    string Name,
    string? Description,
    Guid LocationId,
    string? LocationCode,
    string? LocationName,
    string Status,
    DateTimeOffset RegisteredAt,
    DateTimeOffset LastSeenAt)
{
    public static ItemDto From(Item item, Location? location) => new(
        item.Id.Value,
        item.Sku.Value,
        item.Name,
        item.Description,
        item.LocationId.Value,
        location?.Code.Value,
        location?.Name,
        item.Status.ToString(),
        item.RegisteredAt,
        item.LastSeenAt);
}

/// <summary>Uma linha do histórico já legível: IDs resolvidos e narrativa pronta.</summary>
public sealed record ItemHistoryEntryDto(
    Guid Id,
    int Sequence,
    string Kind,
    DateTimeOffset OccurredAt,
    string Actor,
    string? FromLocation,
    string? ToLocation,
    string? Reason,
    Guid? InventoryCountId,
    string? Note,
    string Narrative);

/// <summary>
/// A resposta de <c>GET /v1/items/{id}/history</c> — a "vida do item" pelo ID,
/// que é exatamente o que vai alimentar a busca semântica.
/// </summary>
public sealed record ItemHistoryDto(ItemDto Item, IReadOnlyList<ItemHistoryEntryDto> Entries);
