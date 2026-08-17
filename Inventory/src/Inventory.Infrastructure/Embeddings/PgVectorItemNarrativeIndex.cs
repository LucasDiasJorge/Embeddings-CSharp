using Inventory.Application.Contracts.Insights;
using Inventory.Application.Ports.Driven;
using Inventory.Domain.Items;
using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;

namespace Inventory.Infrastructure.Embeddings;

/// <summary>
/// Índice semântico real: Ollama gera os vetores, pgvector guarda e ordena por distância de cosseno.
/// </summary>
internal sealed class PgVectorItemNarrativeIndex(
    NarrativeDbContext context,
    OllamaEmbeddingGenerator generator) : IItemNarrativeIndex
{
    public bool IsEnabled => true;

    public async Task IndexAsync(
        IReadOnlyCollection<ItemNarrative> narratives, CancellationToken cancellationToken = default)
    {
        if (narratives.Count == 0)
        {
            return;
        }

        var texts = narratives.Select(narrative => narrative.Text).ToArray();
        var vectors = await generator.EmbedDocumentsAsync(texts, cancellationToken);
        var ids = narratives.Select(narrative => narrative.LedgerEntryId).ToArray();

        // Apaga-e-insere em vez de upsert: torna a reindexação segura de rodar quantas
        // vezes quiser, inclusive depois de trocar de modelo de embedding.
        await context.Narratives
            .Where(record => ids.Contains(record.LedgerEntryId))
            .ExecuteDeleteAsync(cancellationToken);

        context.Narratives.AddRange(narratives.Select((narrative, position) => new ItemNarrativeRecord
        {
            LedgerEntryId = narrative.LedgerEntryId,
            ItemId = narrative.ItemId.Value,
            Sku = narrative.Sku,
            OccurredAt = narrative.OccurredAt,
            Text = narrative.Text,
            Embedding = vectors[position]
        }));

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NarrativeMatch>> SearchAsync(
        string query, ItemId? scope, int top, CancellationToken cancellationToken = default)
    {
        var queryVector = await generator.EmbedQueryAsync(query, cancellationToken);
        var narratives = context.Narratives.AsNoTracking();

        if (scope is { } itemId)
        {
            narratives = narratives.Where(record => record.ItemId == itemId.Value);
        }

        var hits = await narratives
            .Select(record => new
            {
                record.LedgerEntryId,
                record.ItemId,
                record.Sku,
                record.OccurredAt,
                record.Text,
                Distance = record.Embedding.CosineDistance(queryVector)
            })
            .OrderBy(hit => hit.Distance)
            .Take(top)
            .ToListAsync(cancellationToken);

        return
        [
            .. hits.Select(hit => new NarrativeMatch(
                hit.LedgerEntryId,
                new ItemId(hit.ItemId),
                hit.Sku,
                hit.OccurredAt,
                hit.Text,
                // CosineDistance vem em [0, 2]; a similaridade é o complemento.
                Score: 1 - hit.Distance))
        ];
    }
}
