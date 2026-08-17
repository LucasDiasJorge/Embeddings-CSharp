using Inventory.Domain.Common;

namespace Inventory.Domain.Locations;

/// <summary>
/// Onde um item pode estar. Aceita hierarquia via <see cref="ParentId"/>
/// (prédio → sala → prateleira), mas o item sempre aponta para a folha em que está.
/// </summary>
public sealed class Location
{
    public const int MaxNameLength = 160;

    private Location() { } // EF Core

    private Location(LocationId id, LocationCode code, string name, LocationId? parentId, DateTimeOffset createdAt)
    {
        Id = id;
        Code = code;
        Name = name;
        ParentId = parentId;
        IsActive = true;
        CreatedAt = createdAt;
    }

    public LocationId Id { get; private set; }
    public LocationCode Code { get; private set; } = null!;
    public string Name { get; private set; } = string.Empty;
    public LocationId? ParentId { get; private set; }

    /// <summary>Inativa não recebe itens novos, mas continua no histórico dos itens que já passaram por ela.</summary>
    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Location Create(LocationCode code, string name, LocationId? parentId, DateTimeOffset createdAt)
    {
        var id = LocationId.New();

        if (parentId == id)
        {
            throw new DomainException("location.self_parent", "Uma localização não pode ser pai de si mesma.");
        }

        return new Location(id, code, RequireName(name), parentId, createdAt);
    }

    public void Rename(string name) => Name = RequireName(name);

    public void Deactivate()
    {
        if (!IsActive)
        {
            throw new DomainException("location.already_inactive", $"A localização {Code} já está inativa.");
        }

        IsActive = false;
    }

    public void Activate() => IsActive = true;

    private static string RequireName(string name) =>
        Guard.MaxLength(
            Guard.NotBlank(name, "location.name_empty", "O nome da localização é obrigatório."),
            MaxNameLength, "location.name_too_long", $"O nome da localização excede {MaxNameLength} caracteres.");
}
