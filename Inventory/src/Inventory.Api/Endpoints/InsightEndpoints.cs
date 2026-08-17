using Inventory.Api.Http;
using Inventory.Application.Contracts.Insights;
using Inventory.Application.Services;
using Inventory.Domain.Items;

namespace Inventory.Api.Endpoints;

internal static class InsightEndpoints
{
    public static IEndpointRouteBuilder MapInsightEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/items/{id:guid}/insights", async (
            Guid id, ItemInsightService service, CancellationToken cancellationToken) =>
            (await service.GetInsightsAsync(new ItemId(id), cancellationToken)).ToHttp())
        .WithTags("Insights")
        .WithSummary("Leituras derivadas do histórico do item")
        .WithDescription(
            "Estatística pura sobre o razão: há quanto tempo ninguém confirma o item, quantas vezes " +
            "ele já sumiu, por quantas localizações passou, qual é a casa dele. Funciona sem embedding.");

        app.MapPost("/v1/insights/items/search", async (
            ItemPromptSearchQuery query, ItemInsightService service, CancellationToken cancellationToken) =>
            (await service.SearchItemsAsync(query, cancellationToken)).ToHttp())
        .WithTags("Insights")
        .WithSummary("Encontra itens a partir de uma pergunta em linguagem natural")
        .WithDescription(
            "Busca sobre o histórico narrado e agrega o resultado por item, devolvendo cada item " +
            "com localização atual, há quantos dias não é visto e os eventos que justificaram o " +
            "casamento. Exemplos que funcionam: \"notebooks que vivem sumindo do almoxarifado\", " +
            "\"o que foi realocado por acerto de inventário no último trimestre\", " +
            "\"equipamentos parados na sala 204 sem ninguém conferir\".");

        app.MapPost("/v1/insights/search", async (
            NarrativeSearchQuery query, ItemInsightService service, CancellationToken cancellationToken) =>
            (await service.SearchAsync(query, cancellationToken)).ToHttp())
        .WithTags("Insights")
        .WithSummary("Busca semântica no nível do evento")
        .WithDescription(
            "Devolve linhas do razão em vez de itens. Útil quando o que interessa é o fato — e, " +
            "com itemId preenchido, vira \"o que aconteceu com ESTE item que se pareça com isso?\". " +
            "Sem índice vetorial ligado, responde 409 com code=insight.index_disabled, " +
            "deliberadamente, para não confundir 'não configurado' com 'não encontrei nada'.");

        app.MapPost("/v1/insights/reindex", async (
            ItemInsightService service, CancellationToken cancellationToken) =>
            (await service.ReindexAsync(cancellationToken)).ToHttp())
        .WithTags("Insights")
        .WithSummary("Reconstrói o índice semântico a partir do razão")
        .WithDescription(
            "Rode ao ligar o embedding pela primeira vez, para indexar o histórico já acumulado, " +
            "e sempre que trocar de modelo. É idempotente: a chave do vetor é o id da linha do razão.");

        return app;
    }
}
