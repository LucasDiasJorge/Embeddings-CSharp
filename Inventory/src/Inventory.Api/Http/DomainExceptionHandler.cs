using Inventory.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;

namespace Inventory.Api.Http;

/// <summary>
/// Traduz uma violação de invariante do domínio para 422.
/// </summary>
/// <remarks>
/// 422 e não 400 de propósito: o corpo da requisição estava bem formado e foi
/// compreendido — o que o sistema recusa é a operação, porque ela contradiz o estado
/// do mundo (movimentar um item baixado, contar uma contagem já fechada).
/// O <c>code</c> vai no corpo para o cliente tratar sem depender do texto.
/// </remarks>
internal sealed class DomainExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not DomainException domain)
        {
            return false;
        }

        httpContext.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = domain,
            ProblemDetails =
            {
                Title = "Regra de negócio violada",
                Detail = domain.Message,
                Status = StatusCodes.Status422UnprocessableEntity,
                Extensions = { ["code"] = domain.Code }
            }
        });
    }
}
