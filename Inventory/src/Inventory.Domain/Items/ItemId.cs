namespace Inventory.Domain.Items;

/// <summary>
/// Identidade do item — a chave pela qual você reconstrói toda a vida dele.
/// </summary>
public readonly record struct ItemId(Guid Value)
{
    public static ItemId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}
