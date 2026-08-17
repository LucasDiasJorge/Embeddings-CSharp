using Inventory.Domain.Common;
using Inventory.Domain.Counting;
using Inventory.Domain.Locations;

namespace Inventory.Domain.Items;

/// <summary>
/// Raiz de agregação: um ativo físico único e identificável, que está em
/// exatamente UMA localização por vez.
/// </summary>
/// <remarks>
/// A regra central é que <b>nenhuma transição de estado acontece em silêncio</b>:
/// todo método público que muda o item também escreve uma linha no razão
/// (<see cref="PendingLedgerEntries"/>). É impossível movimentar um item sem
/// deixar rastro, e é esse rastro que a busca semântica vai consumir depois.
/// </remarks>
public sealed class Item
{
    public const int MaxNameLength = 200;

    private readonly List<ItemLedgerEntry> _pendingLedgerEntries = [];

    private Item() { } // EF Core

    private Item(ItemId id, Sku sku, string name, string? description, LocationId locationId, DateTimeOffset at)
    {
        Id = id;
        Sku = sku;
        Name = name;
        Description = description;
        LocationId = locationId;
        Status = ItemStatus.Active;
        RegisteredAt = at;
        LastSeenAt = at;
    }

    public ItemId Id { get; private set; }
    public Sku Sku { get; private set; } = null!;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    /// <summary>
    /// Onde o item está. Se o status for <see cref="ItemStatus.Missing"/> ou
    /// <see cref="ItemStatus.Retired"/>, leia como "última localização conhecida".
    /// </summary>
    public LocationId LocationId { get; private set; }

    public ItemStatus Status { get; private set; }
    public DateTimeOffset RegisteredAt { get; private set; }

    /// <summary>
    /// Última vez que alguém confirmou fisicamente o item (cadastro, movimentação ou contagem).
    /// A distância entre isso e hoje é o melhor indicador de "não sei mais onde isso está".
    /// </summary>
    public DateTimeOffset LastSeenAt { get; private set; }

    /// <summary>Último número usado na linha do tempo. Persistido para o razão nunca ter buracos nem repetições.</summary>
    public int LedgerSequence { get; private set; }

    /// <summary>
    /// Linhas de razão geradas nesta unidade de trabalho e ainda não persistidas.
    /// A aplicação drena isso após o commit; o EF ignora esta coleção.
    /// </summary>
    public IReadOnlyList<ItemLedgerEntry> PendingLedgerEntries => _pendingLedgerEntries;

    public void ClearPendingLedgerEntries() => _pendingLedgerEntries.Clear();

    public static Item Register(
        Sku sku, string name, string? description, LocationId locationId, string actor, DateTimeOffset at)
    {
        var item = new Item(ItemId.New(), sku, RequireName(name), Guard.NullIfBlank(description), locationId, at);

        item.Record(LedgerEntryKind.Registered, at, actor,
            from: null, to: locationId, MovementReason.Registration, countId: null, note: null);

        return item;
    }

    /// <summary>Movimentação deliberada, feita fora de uma contagem.</summary>
    public void MoveTo(LocationId destination, MovementReason reason, string actor, string? note, DateTimeOffset at)
    {
        EnsureNotRetired("movimentar");

        if (destination == LocationId)
        {
            throw new DomainException("item.same_location",
                $"O item {Sku} já está nesta localização; não há o que movimentar.");
        }

        var origin = LocationId;
        LocationId = destination;

        // Quem movimenta teve o item em mãos: se estava dado como sumido, foi encontrado.
        Status = ItemStatus.Active;
        LastSeenAt = at;

        Record(LedgerEntryKind.Moved, at, actor, origin, destination, reason, countId: null, note);
    }

    /// <summary>
    /// A contagem encontrou o item onde o sistema esperava. Não muda a localização —
    /// apenas renova a confiança nela, o que já é informação valiosa no histórico.
    /// </summary>
    public void ConfirmedByCount(InventoryCountId countId, string auditor, DateTimeOffset at)
    {
        EnsureNotRetired("conferir");

        Status = ItemStatus.Active;
        LastSeenAt = at;

        Record(LedgerEntryKind.CountConfirmed, at, auditor,
            from: LocationId, to: LocationId, reason: null, countId, note: null);
    }

    /// <summary>
    /// A contagem achou o item numa localização diferente da registrada.
    /// O físico ganha do sistema: o item passa a morar onde ele realmente está.
    /// </summary>
    public void RelocatedByCount(InventoryCountId countId, LocationId foundAt, string auditor, DateTimeOffset at)
    {
        EnsureNotRetired("realocar");

        if (foundAt == LocationId)
        {
            throw new DomainException("item.not_relocated",
                $"O item {Sku} já estava registrado nesta localização; use a confirmação de contagem.");
        }

        var origin = LocationId;
        LocationId = foundAt;
        Status = ItemStatus.Active;
        LastSeenAt = at;

        Record(LedgerEntryKind.Moved, at, auditor, origin, foundAt, MovementReason.CountReconciliation, countId,
            note: "Encontrado em localização divergente durante a contagem.");
    }

    /// <summary>
    /// A contagem esperava o item e não o encontrou. Ele não é apagado nem movido:
    /// vira <see cref="ItemStatus.Missing"/> guardando a última localização conhecida.
    /// </summary>
    public void ReportedMissingByCount(InventoryCountId countId, string auditor, DateTimeOffset at)
    {
        EnsureNotRetired("dar como faltante");

        Status = ItemStatus.Missing;

        // LastSeenAt NÃO é atualizado de propósito: ninguém viu o item.
        Record(LedgerEntryKind.CountMissing, at, auditor,
            from: LocationId, to: null, reason: null, countId,
            note: "Esperado na localização auditada e não encontrado.");
    }

    public void Rename(string name, string? description, string actor, DateTimeOffset at)
    {
        EnsureNotRetired("renomear");

        Name = RequireName(name);
        Description = Guard.NullIfBlank(description);

        Record(LedgerEntryKind.Renamed, at, actor,
            from: null, to: null, reason: null, countId: null, note: null);
    }

    /// <summary>Baixa definitiva. A partir daqui o item é só histórico.</summary>
    public void Retire(string actor, string? note, DateTimeOffset at)
    {
        EnsureNotRetired("baixar");

        Status = ItemStatus.Retired;

        Record(LedgerEntryKind.Retired, at, actor,
            from: LocationId, to: null, reason: null, countId: null, Guard.NullIfBlank(note));
    }

    /// <summary>Itens baixados não entram em contagem — são invisíveis para a auditoria.</summary>
    public bool IsCountable => Status != ItemStatus.Retired;

    private void Record(
        LedgerEntryKind kind, DateTimeOffset at, string actor, LocationId? from, LocationId? to,
        MovementReason? reason, InventoryCountId? countId, string? note)
    {
        LedgerSequence++;

        _pendingLedgerEntries.Add(new ItemLedgerEntry(
            Id, LedgerSequence, kind, at, RequireActor(actor), from, to, reason, countId, note));
    }

    private void EnsureNotRetired(string operation)
    {
        if (Status == ItemStatus.Retired)
        {
            throw new DomainException("item.retired", $"O item {Sku} está baixado; não é possível {operation}.");
        }
    }

    private static string RequireName(string name) =>
        Guard.MaxLength(
            Guard.NotBlank(name, "item.name_empty", "O nome do item é obrigatório."),
            MaxNameLength, "item.name_too_long", $"O nome do item excede {MaxNameLength} caracteres.");

    private static string RequireActor(string actor) =>
        Guard.NotBlank(actor, "item.actor_empty", "É obrigatório identificar quem executou a operação.");
}
