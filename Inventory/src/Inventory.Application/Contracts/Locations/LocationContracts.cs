using Inventory.Domain.Locations;

namespace Inventory.Application.Contracts.Locations;

public sealed record CreateLocationCommand(string Code, string Name, Guid? ParentId);

public sealed record RenameLocationCommand(string Name);

public sealed record LocationDto(
    Guid Id,
    string Code,
    string Name,
    Guid? ParentId,
    bool IsActive,
    DateTimeOffset CreatedAt)
{
    public static LocationDto From(Location location) => new(
        location.Id.Value,
        location.Code.Value,
        location.Name,
        location.ParentId?.Value,
        location.IsActive,
        location.CreatedAt);
}
