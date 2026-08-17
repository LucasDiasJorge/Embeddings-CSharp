namespace Inventory.Domain.Counting;

/// <summary>Identidade de uma contagem de inventário (a "rodada de auditoria").</summary>
public readonly record struct InventoryCountId(Guid Value)
{
    public static InventoryCountId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}
