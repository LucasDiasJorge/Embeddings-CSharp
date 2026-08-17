namespace Inventory.Domain.Locations;

/// <summary>
/// Identidade de uma localização. Tipada de propósito: impede trocar
/// um LocationId por um ItemId numa chamada de método (os dois são Guid).
/// </summary>
public readonly record struct LocationId(Guid Value)
{
    public static LocationId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}
