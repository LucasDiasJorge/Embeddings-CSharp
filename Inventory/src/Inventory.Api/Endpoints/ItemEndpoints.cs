using Inventory.Api.Http;
using Inventory.Application.Contracts.Items;
using Inventory.Application.Services;
using Inventory.Domain.Items;
using Inventory.Domain.Locations;

namespace Inventory.Api.Endpoints;

internal static class ItemEndpoints
{
    public static IEndpointRouteBuilder MapItemEndpoints(this IEndpointRouteBuilder app)
    {
        var items = app.MapGroup("/v1/items").WithTags("Itens");

        items.MapPost("/", async (
            RegisterItemCommand command, ItemService service, CancellationToken cancellationToken) =>
            (await service.RegisterAsync(command, cancellationToken))
                .ToHttp(created => Results.Created($"/v1/items/{created.Id}", created)))
        .WithSummary("Registra um item")
        .WithDescription("Já nasce alocado numa localização e com a primeira linha do razão gravada.");

        items.MapGet("/", async (
            ItemService service,
            CancellationToken cancellationToken,
            string? sku = null,
            Guid? locationId = null,
            ItemStatus? status = null,
            string? search = null,
            int skip = 0,
            int take = 50) =>
        {
            var query = new ItemQuery(
                sku,
                locationId is { } id ? new LocationId(id) : null,
                status,
                search,
                skip,
                take);

            return (await service.SearchAsync(query, cancellationToken)).ToHttp();
        })
        .WithSummary("Busca itens por SKU, localização, status ou texto livre");

        items.MapGet("/{id:guid}", async (
            Guid id, ItemService service, CancellationToken cancellationToken) =>
            (await service.GetAsync(new ItemId(id), cancellationToken)).ToHttp())
        .WithSummary("Detalha um item");

        items.MapGet("/by-sku/{sku}", async (
            string sku, ItemService service, CancellationToken cancellationToken) =>
            (await service.GetBySkuAsync(sku, cancellationToken)).ToHttp())
        .WithSummary("Detalha um item pela etiqueta")
        .WithDescription("Atalho para o coletor, que lê a etiqueta e não conhece GUIDs.");

        items.MapPost("/{id:guid}/move", async (
            Guid id, MoveItemCommand command, ItemService service, CancellationToken cancellationToken) =>
            (await service.MoveAsync(new ItemId(id), command, cancellationToken)).ToHttp())
        .WithSummary("Movimenta o item para outra localização")
        .WithDescription("Movimentar é uma confirmação física: um item que estava como faltante volta a Active.");

        items.MapPatch("/{id:guid}", async (
            Guid id, RenameItemCommand command, ItemService service, CancellationToken cancellationToken) =>
            (await service.RenameAsync(new ItemId(id), command, cancellationToken)).ToHttp())
        .WithSummary("Atualiza nome e descrição do item");

        items.MapPost("/{id:guid}/retire", async (
            Guid id, RetireItemCommand command, ItemService service, CancellationToken cancellationToken) =>
            (await service.RetireAsync(new ItemId(id), command, cancellationToken)).ToHttp())
        .WithSummary("Dá baixa definitiva no item")
        .WithDescription("O item sai das contagens e não movimenta mais, mas o histórico dele permanece íntegro.");

        items.MapGet("/{id:guid}/history", async (
            Guid id, ItemService service, CancellationToken cancellationToken) =>
            (await service.GetHistoryAsync(new ItemId(id), cancellationToken)).ToHttp())
        .WithSummary("A vida inteira do item, pelo ID")
        .WithDescription(
            "Linha do tempo completa do razão, com localizações resolvidas e cada evento já narrado " +
            "em português. É este texto que o índice semântico consome quando o embedding for ligado.");

        return app;
    }
}
