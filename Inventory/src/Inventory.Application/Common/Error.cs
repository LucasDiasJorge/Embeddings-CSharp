namespace Inventory.Application.Common;

/// <summary>Natureza da falha — é o que o adaptador HTTP usa para escolher o status code.</summary>
public enum ErrorType
{
    /// <summary>Entrada malformada. → 400</summary>
    Validation,

    /// <summary>Recurso referenciado não existe. → 404</summary>
    NotFound,

    /// <summary>Estado atual do sistema impede a operação. → 409</summary>
    Conflict
}

/// <summary>
/// Falha esperada de um caso de uso. Diferente de <c>DomainException</c>: aqui não houve
/// tentativa de violar invariante, só um caminho de negócio que não deu certo.
/// </summary>
public sealed record Error(ErrorType Type, string Code, string Message)
{
    public static Error Validation(string code, string message) => new(ErrorType.Validation, code, message);

    public static Error NotFound(string code, string message) => new(ErrorType.NotFound, code, message);

    public static Error Conflict(string code, string message) => new(ErrorType.Conflict, code, message);
}
