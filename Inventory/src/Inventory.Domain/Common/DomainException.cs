namespace Inventory.Domain.Common;

/// <summary>
/// Tentativa de levar o modelo a um estado inválido (ex.: movimentar um item já baixado).
/// Não é erro de entrada do usuário — é o domínio se recusando a mentir sobre o mundo físico.
/// A API traduz para HTTP 422.
/// </summary>
public sealed class DomainException(string code, string message) : Exception(message)
{
    /// <summary>Código estável, pensado para o cliente tratar (ex.: "item.retired").</summary>
    public string Code { get; } = code;
}
