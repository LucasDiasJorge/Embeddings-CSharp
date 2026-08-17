namespace Inventory.Application.Common;

/// <summary>
/// Resultado de um caso de uso: ou o valor, ou o motivo da falha.
/// Evita usar exceção como fluxo de controle para coisas banais ("item não encontrado")
/// e obriga o adaptador a decidir explicitamente o que fazer com o erro.
/// </summary>
public readonly record struct Result<T>
{
    private readonly T? _value;
    private readonly Error? _error;

    private Result(T value)
    {
        _value = value;
        _error = null;
    }

    private Result(Error error)
    {
        _value = default;
        _error = error;
    }

    public bool IsSuccess => _error is null;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException($"Result sem valor: {_error!.Code} — {_error.Message}.");

    public Error Error => _error
        ?? throw new InvalidOperationException("Result bem-sucedido não tem erro.");

    public static Result<T> Success(T value) => new(value);

    public static Result<T> Failure(Error error) => new(error);

    // As conversões implícitas deixam os handlers lerem como prosa:
    //   if (item is null) return Error.NotFound(...);
    //   return ItemDto.From(item);
    public static implicit operator Result<T>(T value) => Success(value);

    public static implicit operator Result<T>(Error error) => Failure(error);

    public Result<TOut> Map<TOut>(Func<T, TOut> map) =>
        IsSuccess ? Result<TOut>.Success(map(_value!)) : Result<TOut>.Failure(_error!);
}
