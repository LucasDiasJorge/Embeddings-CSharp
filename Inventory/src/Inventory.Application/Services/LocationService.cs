using Inventory.Application.Common;
using Inventory.Application.Contracts.Items;
using Inventory.Application.Contracts.Locations;
using Inventory.Application.Ports.Driven;
using Inventory.Domain.Locations;

namespace Inventory.Application.Services;

public sealed class LocationService(
    ILocationRepository locations,
    IItemRepository items,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<Result<LocationDto>> CreateAsync(
        CreateLocationCommand command, CancellationToken cancellationToken = default)
    {
        var code = LocationCode.Create(command.Code);

        if (await locations.FindByCodeAsync(code, cancellationToken) is not null)
        {
            return Error.Conflict("location.duplicate_code", $"Já existe uma localização com o código {code}.");
        }

        LocationId? parentId = null;

        if (command.ParentId is { } rawParentId)
        {
            parentId = new LocationId(rawParentId);

            if (await locations.FindByIdAsync(parentId.Value, cancellationToken) is null)
            {
                return Error.NotFound("location.parent_not_found",
                    $"A localização pai {rawParentId} não existe.");
            }
        }

        var location = Location.Create(code, command.Name, parentId, clock.UtcNow);

        await locations.AddAsync(location, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return LocationDto.From(location);
    }

    public async Task<Result<LocationDto>> RenameAsync(
        LocationId id, RenameLocationCommand command, CancellationToken cancellationToken = default)
    {
        if (await locations.FindByIdAsync(id, cancellationToken) is not { } location)
        {
            return NotFound(id);
        }

        location.Rename(command.Name);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return LocationDto.From(location);
    }

    public async Task<Result<LocationDto>> DeactivateAsync(
        LocationId id, CancellationToken cancellationToken = default)
    {
        if (await locations.FindByIdAsync(id, cancellationToken) is not { } location)
        {
            return NotFound(id);
        }

        // Regra entre agregados, logo mora aqui e não dentro de Location:
        // desativar um lugar que ainda guarda coisas é como perder o rastro delas de propósito.
        var occupancy = await items.CountByLocationAsync(id, cancellationToken);

        if (occupancy > 0)
        {
            return Error.Conflict("location.not_empty",
                $"A localização {location.Code} ainda tem {occupancy} item(ns); movimente-os antes de desativá-la.");
        }

        location.Deactivate();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return LocationDto.From(location);
    }

    public async Task<Result<LocationDto>> GetAsync(LocationId id, CancellationToken cancellationToken = default) =>
        await locations.FindByIdAsync(id, cancellationToken) is { } location
            ? Result<LocationDto>.Success(LocationDto.From(location))
            : Result<LocationDto>.Failure(NotFound(id));

    public async Task<Result<IReadOnlyList<LocationDto>>> ListAsync(
        bool includeInactive, CancellationToken cancellationToken = default)
    {
        var found = await locations.ListAsync(includeInactive, cancellationToken);

        return Result<IReadOnlyList<LocationDto>>.Success([.. found.Select(LocationDto.From)]);
    }

    public async Task<Result<IReadOnlyList<ItemDto>>> ListItemsAsync(
        LocationId id, CancellationToken cancellationToken = default)
    {
        if (await locations.FindByIdAsync(id, cancellationToken) is not { } location)
        {
            return Error.NotFound("location.not_found", $"A localização {id} não existe.");
        }

        var found = await items.ListCountableByLocationAsync(id, cancellationToken);

        return Result<IReadOnlyList<ItemDto>>.Success([.. found.Select(item => ItemDto.From(item, location))]);
    }

    private static Error NotFound(LocationId id) =>
        Error.NotFound("location.not_found", $"A localização {id} não existe.");
}
