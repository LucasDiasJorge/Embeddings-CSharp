using Inventory.Domain.Common;
using Inventory.Domain.Items;
using Inventory.Domain.Locations;

namespace Inventory.Domain.Counting;

/// <summary>
/// Raiz de agregação da <b>contagem de inventário</b> — a "regra do auditor".
/// </summary>
/// <remarks>
/// O ciclo é sempre o mesmo, e é ele que dá o nome ao conceito:
/// <list type="number">
///   <item><b>Abrir</b> numa localização: congela a lista de itens que o sistema acredita estarem lá.</item>
///   <item><b>Ler</b> (scan) o que o auditor realmente encontra, item por item.</item>
///   <item><b>Fechar</b>: confronta as duas listas e devolve a <see cref="CountReconciliation"/>.</item>
/// </list>
/// A contagem em si nunca toca nos itens — ela só produz o veredito. Quem aplica o
/// veredito é o caso de uso <c>InventoryCountService.CloseAsync</c>, chamando os métodos
/// do agregado <see cref="Item"/>. Essa separação é o que permite fechar uma contagem
/// em memória, num teste, sem banco nenhum.
/// </remarks>
public sealed class InventoryCount
{
    public const int MaxAuditorLength = 120;

    private readonly List<ExpectedItem> _expected = [];
    private readonly List<CountScan> _scans = [];

    private InventoryCount() { } // EF Core

    private InventoryCount(
        InventoryCountId id, LocationId locationId, string auditor, IEnumerable<ItemId> expectedItems, DateTimeOffset at)
    {
        Id = id;
        LocationId = locationId;
        Auditor = auditor;
        Status = InventoryCountStatus.Open;
        OpenedAt = at;

        _expected.AddRange(expectedItems.Distinct().Select(itemId => new ExpectedItem(itemId)));
    }

    public InventoryCountId Id { get; private set; }

    /// <summary>A localização sob auditoria. Uma contagem audita uma localização por vez.</summary>
    public LocationId LocationId { get; private set; }

    public string Auditor { get; private set; } = string.Empty;
    public InventoryCountStatus Status { get; private set; }
    public DateTimeOffset OpenedAt { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }
    public string? CancellationReason { get; private set; }

    /// <summary>Fotografia tirada na abertura — o que o sistema achava que estava aqui.</summary>
    public IReadOnlyList<ExpectedItem> Expected => _expected;

    /// <summary>O que o auditor efetivamente encontrou.</summary>
    public IReadOnlyList<CountScan> Scans => _scans;

    public bool IsOpen => Status == InventoryCountStatus.Open;

    public static InventoryCount Open(
        LocationId locationId, string auditor, IEnumerable<ItemId> expectedItems, DateTimeOffset at) =>
        new(InventoryCountId.New(), locationId, RequireAuditor(auditor), expectedItems, at);

    /// <summary>
    /// Registra uma leitura. É idempotente de propósito: bipar a mesma etiqueta duas
    /// vezes é rotina em campo e não pode virar erro nem duplicar linha.
    /// </summary>
    public void Scan(ItemId itemId, DateTimeOffset at)
    {
        EnsureOpen();

        if (_scans.Any(scan => scan.ItemId == itemId))
        {
            return;
        }

        _scans.Add(new CountScan(itemId, at));
    }

    /// <summary>Desfaz uma leitura enquanto a contagem está aberta (bipou a prateleira errada).</summary>
    public bool RemoveScan(ItemId itemId)
    {
        EnsureOpen();
        return _scans.RemoveAll(scan => scan.ItemId == itemId) > 0;
    }

    /// <summary>
    /// Confronta esperados × lidos sem alterar nada. Como esperados e leituras ficam
    /// gravados, o veredito de uma contagem fechada é sempre recalculável — é o que
    /// permite reemitir o relatório meses depois e obter exatamente o mesmo resultado.
    /// </summary>
    public CountReconciliation Reconcile()
    {
        var expected = _expected.Select(item => item.ItemId).ToHashSet();
        var scanned = _scans.Select(scan => scan.ItemId).ToHashSet();

        return new CountReconciliation(
            Confirmed: [.. expected.Intersect(scanned)],
            Missing: [.. expected.Except(scanned)],
            Unexpected: [.. scanned.Except(expected)]);
    }

    /// <summary>
    /// Fecha a contagem e devolve o veredito. A contagem fica imutável a partir daqui —
    /// é isso que faz dela um documento de auditoria e não um rascunho.
    /// </summary>
    public CountReconciliation Close(DateTimeOffset at)
    {
        EnsureOpen();

        var reconciliation = Reconcile();

        Status = InventoryCountStatus.Closed;
        ClosedAt = at;

        return reconciliation;
    }

    public void Cancel(string? reason, DateTimeOffset at)
    {
        EnsureOpen();

        Status = InventoryCountStatus.Cancelled;
        ClosedAt = at;
        CancellationReason = Guard.NullIfBlank(reason);
    }

    private void EnsureOpen()
    {
        if (!IsOpen)
        {
            throw new DomainException("count.not_open",
                $"A contagem {Id} está {Status} e não aceita mais alterações.");
        }
    }

    private static string RequireAuditor(string auditor) =>
        Guard.MaxLength(
            Guard.NotBlank(auditor, "count.auditor_empty", "É obrigatório identificar o auditor da contagem."),
            MaxAuditorLength, "count.auditor_too_long", $"O nome do auditor excede {MaxAuditorLength} caracteres.");
}
