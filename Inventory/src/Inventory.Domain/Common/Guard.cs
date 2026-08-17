namespace Inventory.Domain.Common;

internal static class Guard
{
    public static string NotBlank(string? value, string code, string message)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? throw new DomainException(code, message) : trimmed;
    }

    public static string MaxLength(string value, int max, string code, string message) =>
        value.Length > max ? throw new DomainException(code, message) : value;

    /// <summary>Texto opcional: espaços em branco viram null, para não guardar "" no banco.</summary>
    public static string? NullIfBlank(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }
}
