namespace Inventory.Application.Contracts.Counting;

public sealed record OpenInventoryCountCommand(Guid LocationId, string Auditor);

/// <summary>
/// Uma leitura. Aceita <paramref name="Sku"/> porque em campo o auditor bipa a etiqueta,
/// não digita um GUID; <paramref name="ItemId"/> fica para integrações.
/// </summary>
public sealed record ScanItemCommand(string? Sku, Guid? ItemId);

public sealed record CancelInventoryCountCommand(string? Reason);

public sealed record CountedItemDto(Guid ItemId, string Sku, string Name, DateTimeOffset? ScannedAt);

/// <summary>
/// Estado ao vivo da contagem. As três listas são disjuntas: cada item aparece
/// em exatamente uma delas, então dá para renderizar a tela do coletor direto.
/// </summary>
/// <param name="Pending">Esperados que ainda não foram bipados.</param>
/// <param name="Confirmed">Esperados e já bipados.</param>
/// <param name="Unexpected">Bipados aqui sem serem esperados — serão realocados no fechamento.</param>
public sealed record InventoryCountDto(
    Guid Id,
    Guid LocationId,
    string LocationCode,
    string LocationName,
    string Auditor,
    string Status,
    DateTimeOffset OpenedAt,
    DateTimeOffset? ClosedAt,
    string? CancellationReason,
    int ExpectedCount,
    int ScannedCount,
    IReadOnlyList<CountedItemDto> Pending,
    IReadOnlyList<CountedItemDto> Confirmed,
    IReadOnlyList<CountedItemDto> Unexpected);

public sealed record InventoryCountSummaryDto(
    Guid Id,
    Guid LocationId,
    string LocationCode,
    string Auditor,
    string Status,
    DateTimeOffset OpenedAt,
    DateTimeOffset? ClosedAt,
    int ExpectedCount,
    int ScannedCount);

/// <summary>
/// O documento de fechamento: o que a auditoria concluiu e o que ela mexeu nos itens.
/// </summary>
/// <param name="Accuracy">Confirmados ÷ esperados. A métrica que o gestor cobra.</param>
public sealed record InventoryCountReportDto(
    Guid CountId,
    Guid LocationId,
    string LocationCode,
    string Auditor,
    DateTimeOffset OpenedAt,
    DateTimeOffset ClosedAt,
    double Accuracy,
    bool IsClean,
    IReadOnlyList<CountedItemDto> Confirmed,
    IReadOnlyList<CountedItemDto> Missing,
    IReadOnlyList<CountedItemDto> Relocated);
