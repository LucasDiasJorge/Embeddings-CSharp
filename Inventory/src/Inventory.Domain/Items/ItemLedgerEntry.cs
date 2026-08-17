using Inventory.Domain.Counting;
using Inventory.Domain.Locations;

namespace Inventory.Domain.Items;

/// <summary>
/// Uma linha imutável do razão do item: o registro append-only de tudo que
/// aconteceu com ele. Nada aqui é atualizado ou apagado — é a fonte da verdade
/// do histórico e o substrato da busca semântica futura.
/// </summary>
/// <remarks>
/// Guarda apenas <b>fatos estruturados</b>. A narrativa em linguagem natural que
/// será embedada é composta na camada de aplicação (<c>ItemNarrator</c>), onde os
/// nomes das localizações estão disponíveis — "movido do Almoxarifado A3 para a
/// Sala 204" gera um vetor muito melhor do que dois GUIDs.
/// </remarks>
public sealed class ItemLedgerEntry
{
    private ItemLedgerEntry() { } // EF Core

    internal ItemLedgerEntry(
        ItemId itemId,
        int sequence,
        LedgerEntryKind kind,
        DateTimeOffset occurredAt,
        string actor,
        LocationId? fromLocationId,
        LocationId? toLocationId,
        MovementReason? reason,
        InventoryCountId? inventoryCountId,
        string? note)
    {
        Id = Guid.CreateVersion7();
        ItemId = itemId;
        Sequence = sequence;
        Kind = kind;
        OccurredAt = occurredAt;
        Actor = actor;
        FromLocationId = fromLocationId;
        ToLocationId = toLocationId;
        Reason = reason;
        InventoryCountId = inventoryCountId;
        Note = note;
    }

    public Guid Id { get; private set; }
    public ItemId ItemId { get; private set; }

    /// <summary>Posição na linha do tempo do item (1, 2, 3...). Ordena sem depender de relógio.</summary>
    public int Sequence { get; private set; }

    public LedgerEntryKind Kind { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }

    /// <summary>Quem provocou o evento (usuário, auditor, integração).</summary>
    public string Actor { get; private set; } = string.Empty;

    public LocationId? FromLocationId { get; private set; }
    public LocationId? ToLocationId { get; private set; }
    public MovementReason? Reason { get; private set; }

    /// <summary>Preenchido quando o evento nasceu de uma contagem — liga o fato à auditoria que o gerou.</summary>
    public InventoryCountId? InventoryCountId { get; private set; }

    public string? Note { get; private set; }
}
