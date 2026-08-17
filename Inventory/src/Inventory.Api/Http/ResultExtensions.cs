using Inventory.Application.Common;

namespace Inventory.Api.Http;

/// <summary>
/// Único ponto onde o vocabulário da aplicação vira vocabulário HTTP.
/// Nenhum endpoint decide status code por conta própria — o tipo do erro decide.
/// </summary>
internal static class ResultExtensions
{
    public static IResult ToHttp<T>(this Result<T> result) =>
        result.IsSuccess ? Results.Ok(result.Value) : result.Error.ToProblem();

    public static IResult ToHttp<T>(this Result<T> result, Func<T, IResult> onSuccess) =>
        result.IsSuccess ? onSuccess(result.Value) : result.Error.ToProblem();

    private static IResult ToProblem(this Error error) => Results.Problem(
        title: error.Type switch
        {
            ErrorType.NotFound => "Recurso não encontrado",
            ErrorType.Conflict => "Operação em conflito com o estado atual",
            _ => "Requisição inválida"
        },
        detail: error.Message,
        statusCode: error.Type switch
        {
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        },
        extensions: new Dictionary<string, object?> { ["code"] = error.Code });
}
