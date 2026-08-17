using Inventory.Domain.Common;

namespace Inventory.Domain.Locations;

/// <summary>
/// Código operacional da localização — o que está impresso na etiqueta da prateleira
/// (ex.: "ALM-A3", "SALA-204"). Normalizado em maiúsculas para que a leitura do
/// coletor bata com o cadastro independentemente de como foi digitado.
/// </summary>
public sealed record LocationCode
{
    public const int MaxLength = 40;

    private LocationCode(string value) => Value = value;

    public string Value { get; }

    public static LocationCode Create(string? value)
    {
        var normalized = Guard.NotBlank(value, "location_code.empty", "O código da localização é obrigatório.")
            .ToUpperInvariant();

        return new LocationCode(Guard.MaxLength(normalized, MaxLength, "location_code.too_long",
            $"O código da localização excede {MaxLength} caracteres."));
    }

    public override string ToString() => Value;
}
