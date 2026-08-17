using Inventory.Api.Http;
using Inventory.Application.Contracts.Locations;
using Inventory.Application.Services;
using Inventory.Domain.Locations;

namespace Inventory.Api.Endpoints;

internal static class LocationEndpoints
{
    public static IEndpointRouteBuilder MapLocationEndpoints(this IEndpointRouteBuilder app)
    {
        var locations = app.MapGroup("/v1/locations").WithTags("Localizações");

        locations.MapPost("/", async (
            CreateLocationCommand command, LocationService service, CancellationToken cancellationToken) =>
            (await service.CreateAsync(command, cancellationToken))
                .ToHttp(created => Results.Created($"/v1/locations/{created.Id}", created)))
        .WithSummary("Cadastra uma localização")
        .WithDescription("O código é normalizado em maiúsculas e é único. ParentId permite hierarquia (prédio → sala → prateleira).");

        locations.MapGet("/", async (
            LocationService service, CancellationToken cancellationToken, bool includeInactive = false) =>
            (await service.ListAsync(includeInactive, cancellationToken)).ToHttp())
        .WithSummary("Lista as localizações");

        locations.MapGet("/{id:guid}", async (
            Guid id, LocationService service, CancellationToken cancellationToken) =>
            (await service.GetAsync(new LocationId(id), cancellationToken)).ToHttp())
        .WithSummary("Detalha uma localização");

        locations.MapPatch("/{id:guid}", async (
            Guid id,
            RenameLocationCommand command,
            LocationService service,
            CancellationToken cancellationToken) =>
            (await service.RenameAsync(new LocationId(id), command, cancellationToken)).ToHttp())
        .WithSummary("Renomeia uma localização");

        locations.MapPost("/{id:guid}/deactivate", async (
            Guid id, LocationService service, CancellationToken cancellationToken) =>
            (await service.DeactivateAsync(new LocationId(id), cancellationToken)).ToHttp())
        .WithSummary("Desativa uma localização")
        .WithDescription("Recusa com 409 enquanto houver itens alocados: desativar um lugar cheio perderia o rastro do que está nele.");

        locations.MapGet("/{id:guid}/items", async (
            Guid id, LocationService service, CancellationToken cancellationToken) =>
            (await service.ListItemsAsync(new LocationId(id), cancellationToken)).ToHttp())
        .WithSummary("Itens que o sistema acredita estarem aqui")
        .WithDescription("É a prévia exata do que uma contagem aberta nesta localização vai cobrar do auditor.");

        return app;
    }
}
