namespace Inventory.Domain.Items;

/// <summary>O que aconteceu com o item numa linha do razão.</summary>
public enum LedgerEntryKind
{
    Registered = 0,
    Moved = 1,

    /// <summary>Uma contagem encontrou o item exatamente onde o sistema dizia que ele estaria.</summary>
    CountConfirmed = 2,

    /// <summary>Uma contagem esperava o item na localização auditada e não o encontrou.</summary>
    CountMissing = 3,

    Renamed = 4,
    Retired = 5
}
