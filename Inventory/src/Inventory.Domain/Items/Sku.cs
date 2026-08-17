using Inventory.Domain.Common;

namespace Inventory.Domain.Items;

/// <summary>
/// A etiqueta física do item (patrimônio, código de barras, QR). É por ela que o
/// auditor identifica o item no chão de fábrica, então é única e normalizada.
/// </summary>
public sealed record Sku
{
    public const int MaxLength = 64;

    private Sku(string value) => Value = value;

    public string Value { get; }

    public static Sku Create(string? value)
    {
        var normalized = Guard.NotBlank(value, "sku.empty", "O SKU/etiqueta do item é obrigatório.")
            .ToUpperInvariant();

        return new Sku(Guard.MaxLength(normalized, MaxLength, "sku.too_long",
            $"O SKU excede {MaxLength} caracteres."));
    }

    public override string ToString() => Value;
}
