using Inventory.Domain.Items;

namespace Inventory.Application.Contracts.Insights;

/// <summary>Pergunta em linguagem natural que devolve itens ranqueados.</summary>
public sealed record ItemPromptSearchQuery(string Prompt, int Top = 5);

/// <summary>
/// Busca no nível do evento. Com <paramref name="ItemId"/> preenchido, restringe
/// ao histórico de um item só.
/// </summary>
public sealed record NarrativeSearchQuery(string Query, Guid? ItemId, int Top = 5);

/// <summary>
/// Uma linha do razão traduzida para linguagem natural, pronta para virar vetor.
/// </summary>
/// <param name="LedgerEntryId">Chave estável: o vetor sempre aponta de volta para o fato que o originou.</param>
/// <param name="Text">
/// O texto que será embedado. É aqui que mora a qualidade da busca —
/// "movido do Almoxarifado A3 para a Sala 204 por João em 08/08/2026, motivo: acerto de
/// inventário" gera um vetor útil; dois GUIDs, não.
/// </param>
public sealed record ItemNarrative(
    Guid LedgerEntryId,
    ItemId ItemId,
    string Sku,
    DateTimeOffset OccurredAt,
    string Text);

/// <param name="Score">Similaridade normalizada 0..1 — quanto maior, mais próximo da pergunta.</param>
public sealed record NarrativeMatch(
    Guid LedgerEntryId,
    ItemId ItemId,
    string Sku,
    DateTimeOffset OccurredAt,
    string Text,
    double Score);

/// <summary>
/// Um item encontrado por uma pergunta em linguagem natural.
/// </summary>
/// <remarks>
/// Vem com <paramref name="Evidence"/> de propósito: num sistema de auditoria, "o modelo
/// achou que sim" não serve como resposta. O usuário precisa ver QUAIS eventos do histórico
/// casaram com a pergunta para decidir se concorda — e é isso que transforma a busca
/// semântica em ferramenta de trabalho em vez de adivinhação.
/// </remarks>
/// <param name="Score">Similaridade do melhor evento casado (0..1).</param>
/// <param name="MatchCount">Quantos eventos do histórico deste item entraram no resultado. Corroboração.</param>
public sealed record ItemSearchHit(
    Guid ItemId,
    string Sku,
    string Name,
    string Status,
    string CurrentLocation,
    DateTimeOffset LastSeenAt,
    int DaysSinceLastSeen,
    double Score,
    int MatchCount,
    IReadOnlyList<NarrativeMatch> Evidence);

public sealed record ReindexReportDto(int IndexedEntries, int Batches, double DurationSeconds);

/// <summary>
/// Leitura derivada do razão, calculada sem depender de embedding nenhum.
/// Serve de linha de base honesta: boa parte do que se pede a um "insight de IA"
/// é, na verdade, contagem e data.
/// </summary>
public sealed record ItemInsightsDto(
    Guid ItemId,
    string Sku,
    string Name,
    string Status,
    string CurrentLocation,
    DateTimeOffset LastSeenAt,
    int DaysSinceLastSeen,
    int TotalMovements,
    int TimesCounted,
    int TimesReportedMissing,
    int DistinctLocations,
    string? MostFrequentLocation,
    double? AverageDaysBetweenMovements,
    IReadOnlyList<string> Highlights);
