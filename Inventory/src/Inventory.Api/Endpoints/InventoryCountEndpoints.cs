using Inventory.Api.Http;
using Inventory.Application.Contracts.Counting;
using Inventory.Application.Services;
using Inventory.Domain.Counting;
using Inventory.Domain.Locations;

namespace Inventory.Api.Endpoints;

/// <summary>
/// O fluxo do auditor: abrir → bipar → fechar.
/// </summary>
internal static class InventoryCountEndpoints
{
    public static IEndpointRouteBuilder MapInventoryCountEndpoints(this IEndpointRouteBuilder app)
    {
        var counts = app.MapGroup("/v1/inventory-counts").WithTags("Contagens de inventário");

        counts.MapPost("/", async (
            OpenInventoryCountCommand command,
            InventoryCountService service,
            CancellationToken cancellationToken) =>
            (await service.OpenAsync(command, cancellationToken))
                .ToHttp(opened => Results.Created($"/v1/inventory-counts/{opened.Id}", opened)))
        .WithSummary("Abre uma contagem numa localização")
        .WithDescription(
            "Congela a lista de itens que o sistema acredita estarem lá. A partir daqui, movimentações " +
            "feitas por terceiros não alteram o que será cobrado do auditor. Recusa com 409 se já houver " +
            "contagem aberta na mesma localização.");

        counts.MapGet("/", async (
            InventoryCountService service,
            CancellationToken cancellationToken,
            Guid? locationId = null,
            InventoryCountStatus? status = null) =>
            (await service.ListAsync(
                locationId is { } id ? new LocationId(id) : null, status, cancellationToken)).ToHttp())
        .WithSummary("Lista contagens");

        counts.MapGet("/{id:guid}", async (
            Guid id, InventoryCountService service, CancellationToken cancellationToken) =>
            (await service.GetAsync(new InventoryCountId(id), cancellationToken)).ToHttp())
        .WithSummary("Estado ao vivo da contagem")
        .WithDescription("Devolve três listas disjuntas: pendentes (falta bipar), confirmados e inesperados.");

        counts.MapPost("/{id:guid}/scans", async (
            Guid id,
            ScanItemCommand command,
            InventoryCountService service,
            CancellationToken cancellationToken) =>
            (await service.ScanAsync(new InventoryCountId(id), command, cancellationToken)).ToHttp())
        .WithSummary("Registra uma leitura")
        .WithDescription(
            "Idempotente: bipar a mesma etiqueta de novo não duplica nada. Ler um item que não era " +
            "esperado aqui é permitido — ele entra como inesperado e será realocado no fechamento.");

        counts.MapDelete("/{id:guid}/scans/{itemId:guid}", async (
            Guid id, Guid itemId, InventoryCountService service, CancellationToken cancellationToken) =>
            (await service.RemoveScanAsync(new InventoryCountId(id), itemId, cancellationToken)).ToHttp())
        .WithSummary("Desfaz uma leitura");

        counts.MapPost("/{id:guid}/close", async (
            Guid id, InventoryCountService service, CancellationToken cancellationToken) =>
            (await service.CloseAsync(new InventoryCountId(id), cancellationToken)).ToHttp())
        .WithSummary("Fecha a contagem e aplica o veredito")
        .WithDescription(
            "Irreversível. Confirmados renovam o LastSeenAt; esperados e não encontrados viram Missing; " +
            "inesperados são realocados para esta localização com motivo CountReconciliation. " +
            "Cada uma dessas consequências vira uma linha no razão do item.");

        counts.MapPost("/{id:guid}/cancel", async (
            Guid id,
            CancelInventoryCountCommand command,
            InventoryCountService service,
            CancellationToken cancellationToken) =>
            (await service.CancelAsync(new InventoryCountId(id), command, cancellationToken)).ToHttp())
        .WithSummary("Cancela a contagem")
        .WithDescription("Descarta a rodada sem tocar em nenhum item.");

        counts.MapGet("/{id:guid}/report", async (
            Guid id, InventoryCountService service, CancellationToken cancellationToken) =>
            (await service.GetReportAsync(new InventoryCountId(id), cancellationToken)).ToHttp())
        .WithSummary("Relatório de uma contagem fechada")
        .WithDescription("Recalculado a partir dos esperados e das leituras gravados — sempre reproduz o mesmo resultado.");

        return app;
    }
}
