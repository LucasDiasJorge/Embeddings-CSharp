using Inventory.Application.Common;
using Inventory.Application.Contracts.Insights;
using Inventory.Application.Narration;
using Inventory.Application.Ports.Driven;
using Inventory.Domain.Items;
using Inventory.Domain.Locations;

namespace Inventory.Application.Services;

/// <summary>
/// Leituras derivadas do razão.
/// </summary>
/// <remarks>
/// <see cref="GetInsightsAsync"/> é deliberadamente burro: só conta e divide o que já está
/// gravado. Vale a pena tê-lo antes do embedding porque boa parte do que se pede a uma
/// "IA de inventário" — há quanto tempo não vejo isso, o que mais some, o que vive mudando
/// de lugar — é aritmética sobre um histórico bem registrado. O embedding entra depois,
/// para as perguntas que aritmética não responde, e entra por <see cref="SearchAsync"/>.
/// </remarks>
public sealed class ItemInsightService(
    IItemRepository items,
    IItemLedgerRepository ledger,
    ILocationRepository locations,
    IItemNarrativeIndex index,
    ItemNarrator narrator,
    IClock clock)
{
    private const int StaleAfterDays = 90;

    /// <summary>Quantos eventos justificativos acompanham cada item no resultado da busca.</summary>
    private const int EvidencePerItem = 3;

    /// <summary>
    /// Folga de candidatos pedida ao índice. Um item muito relevante casa com vários
    /// eventos e ocuparia várias vagas do top-N; sem folga, itens bons ficariam de fora
    /// por causa do vizinho falante.
    /// </summary>
    private const int CandidateFactor = 6;

    private const int ReindexPageSize = 200;

    public async Task<Result<ItemInsightsDto>> GetInsightsAsync(
        ItemId itemId, CancellationToken cancellationToken = default)
    {
        if (await items.FindByIdAsync(itemId, cancellationToken) is not { } item)
        {
            return Error.NotFound("item.not_found", $"O item {itemId} não existe.");
        }

        var entries = await ledger.ListByItemAsync(itemId, cancellationToken);

        var visited = entries
            .Select(entry => entry.ToLocationId)
            .OfType<LocationId>()
            .ToArray();

        var byId = await locations.FindManyAsync(
            visited.Append(item.LocationId).Distinct(), cancellationToken);

        var movements = entries.Where(entry => entry.Kind == LedgerEntryKind.Moved).ToArray();
        var timesCounted = entries.Count(entry => entry.Kind == LedgerEntryKind.CountConfirmed);
        var timesMissing = entries.Count(entry => entry.Kind == LedgerEntryKind.CountMissing);
        var daysSinceLastSeen = (int)(clock.UtcNow - item.LastSeenAt).TotalDays;

        var favourite = visited
            .GroupBy(id => id)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key.Value)
            .Select(group => byId.GetValueOrDefault(group.Key))
            .FirstOrDefault();

        return new ItemInsightsDto(
            item.Id.Value,
            item.Sku.Value,
            item.Name,
            item.Status.ToString(),
            Describe(byId.GetValueOrDefault(item.LocationId)),
            item.LastSeenAt,
            daysSinceLastSeen,
            movements.Length,
            timesCounted,
            timesMissing,
            visited.Distinct().Count(),
            favourite is null ? null : Describe(favourite),
            AverageDaysBetween(movements),
            BuildHighlights(item, movements.Length, timesCounted, timesMissing, daysSinceLastSeen));
    }

    public async Task<Result<IReadOnlyList<ItemSearchHit>>> SearchItemsAsync(
        ItemPromptSearchQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.Prompt))
        {
            return Error.Validation("insight.empty_query", "Informe a pergunta que descreve os itens procurados.");
        }

        if (!index.IsEnabled)
        {
            return IndexDisabled();
        }

        var wanted = Math.Clamp(query.Top, 1, 25);
        var matches = await index.SearchAsync(query.Prompt, scope: null, wanted * CandidateFactor, cancellationToken);

        if (matches.Count == 0)
        {
            return Result<IReadOnlyList<ItemSearchHit>>.Success([]);
        }

        // O ranking é o melhor evento de cada item, com a quantidade de eventos casados
        // como desempate. Poderia haver uma fórmula mais esperta, mas num sistema de
        // auditoria vale mais uma ordenação que o usuário consegue explicar em voz alta.
        var ranked = matches
            .GroupBy(match => match.ItemId)
            .Select(group => new
            {
                ItemId = group.Key,
                Score = group.Max(match => match.Score),
                MatchCount = group.Count(),
                Evidence = group.OrderByDescending(match => match.Score).Take(EvidencePerItem).ToArray()
            })
            .OrderByDescending(candidate => candidate.Score)
            .ThenByDescending(candidate => candidate.MatchCount)
            .Take(wanted)
            .ToArray();

        var found = await items.FindManyAsync(ranked.Select(candidate => candidate.ItemId), cancellationToken);
        var byId = found.ToDictionary(item => item.Id);
        var byLocation = await locations.FindManyAsync(
            found.Select(item => item.LocationId).Distinct(), cancellationToken);

        var now = clock.UtcNow;

        return Result<IReadOnlyList<ItemSearchHit>>.Success(
        [
            .. ranked
                .Where(candidate => byId.ContainsKey(candidate.ItemId))
                .Select(candidate =>
                {
                    var item = byId[candidate.ItemId];

                    return new ItemSearchHit(
                        item.Id.Value,
                        item.Sku.Value,
                        item.Name,
                        item.Status.ToString(),
                        Describe(byLocation.GetValueOrDefault(item.LocationId)),
                        item.LastSeenAt,
                        (int)(now - item.LastSeenAt).TotalDays,
                        candidate.Score,
                        candidate.MatchCount,
                        candidate.Evidence);
                })
        ]);
    }

    public async Task<Result<IReadOnlyList<NarrativeMatch>>> SearchAsync(
        NarrativeSearchQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.Query))
        {
            return Error.Validation("insight.empty_query", "Informe a pergunta a ser buscada no histórico.");
        }

        // Sem adaptador vetorial plugado, o honesto é dizer que não dá — e não
        // devolver uma lista vazia que passa por "não encontrei nada".
        if (!index.IsEnabled)
        {
            return Error.Conflict("insight.index_disabled", IndexDisabledMessage);
        }

        var scope = query.ItemId is { } id ? new ItemId(id) : (ItemId?)null;
        var matches = await index.SearchAsync(query.Query, scope, Math.Clamp(query.Top, 1, 50), cancellationToken);

        return Result<IReadOnlyList<NarrativeMatch>>.Success(matches);
    }

    public async Task<Result<ReindexReportDto>> ReindexAsync(CancellationToken cancellationToken = default)
    {
        if (!index.IsEnabled)
        {
            return Error.Conflict("insight.index_disabled", IndexDisabledMessage);
        }

        var startedAt = clock.UtcNow;
        var indexed = 0;
        var batches = 0;

        for (var skip = 0; ; skip += ReindexPageSize)
        {
            var page = await ledger.ListPageAsync(skip, ReindexPageSize, cancellationToken);

            if (page.Count == 0)
            {
                break;
            }

            var narratives = await NarrateAsync(page, cancellationToken);
            await index.IndexAsync(narratives, cancellationToken);

            indexed += narratives.Count;
            batches++;
        }

        return new ReindexReportDto(indexed, batches, (clock.UtcNow - startedAt).TotalSeconds);
    }

    /// <summary>
    /// Reconstrói as narrativas de um lote do razão. Nada é lido de um cache: a narrativa
    /// é sempre derivada dos fatos, o que permite melhorar o texto e reindexar sem
    /// perder nem reescrever histórico.
    /// </summary>
    private async Task<IReadOnlyList<ItemNarrative>> NarrateAsync(
        IReadOnlyList<ItemLedgerEntry> entries, CancellationToken cancellationToken)
    {
        var found = await items.FindManyAsync(
            entries.Select(entry => entry.ItemId).Distinct(), cancellationToken);

        var byId = found.ToDictionary(item => item.Id);

        var referenced = entries
            .SelectMany(entry => new[] { entry.FromLocationId, entry.ToLocationId })
            .OfType<LocationId>()
            .Distinct();

        var byLocation = await locations.FindManyAsync(referenced, cancellationToken);

        return
        [
            .. entries
                .Where(entry => byId.ContainsKey(entry.ItemId))
                .Select(entry => narrator.ToNarrative(entry, byId[entry.ItemId], byLocation))
        ];
    }

    private const string IndexDisabledMessage =
        "A busca semântica exige o adaptador de embeddings ligado (Embeddings:Enabled=true, " +
        "Ollama no ar e pgvector disponível). Enquanto isso, use GET /v1/items/{id}/history " +
        "ou GET /v1/items/{id}/insights, que funcionam sem embedding.";

    private static Error IndexDisabled() => Error.Conflict("insight.index_disabled", IndexDisabledMessage);

    private static IReadOnlyList<string> BuildHighlights(
        Item item, int movements, int timesCounted, int timesMissing, int daysSinceLastSeen)
    {
        var highlights = new List<string>();

        if (item.Status == ItemStatus.Missing)
        {
            highlights.Add("Item consta como faltante desde a última contagem da sua localização.");
        }

        if (item.Status != ItemStatus.Retired && daysSinceLastSeen >= StaleAfterDays)
        {
            highlights.Add($"Sem confirmação física há {daysSinceLastSeen} dias — candidato a uma contagem.");
        }

        if (timesCounted == 0 && item.Status != ItemStatus.Retired)
        {
            highlights.Add("Nunca passou por uma contagem: a localização registrada nunca foi verificada.");
        }

        if (timesMissing > 1)
        {
            highlights.Add($"Já sumiu e reapareceu {timesMissing} vezes — pode indicar processo de retirada informal.");
        }

        if (movements >= 10)
        {
            highlights.Add($"Alta rotatividade: {movements} movimentações registradas.");
        }

        return highlights;
    }

    private static double? AverageDaysBetween(IReadOnlyList<ItemLedgerEntry> movements)
    {
        if (movements.Count < 2)
        {
            return null;
        }

        var ordered = movements.OrderBy(entry => entry.Sequence).ToArray();
        var span = ordered[^1].OccurredAt - ordered[0].OccurredAt;

        return Math.Round(span.TotalDays / (ordered.Length - 1), 1);
    }

    private static string Describe(Location? location) =>
        location is null ? "localização desconhecida" : $"{location.Name} ({location.Code.Value})";
}
