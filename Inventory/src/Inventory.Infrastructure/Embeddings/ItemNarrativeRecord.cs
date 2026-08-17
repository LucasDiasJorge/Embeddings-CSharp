using Pgvector;

namespace Inventory.Infrastructure.Embeddings;

/// <summary>
/// Linha da tabela de vetores. É um modelo de persistência do adaptador, não do domínio —
/// por isso mora aqui e não em Inventory.Domain: o domínio não sabe o que é um vetor.
/// </summary>
/// <remarks>
/// A chave é o id da linha do razão, e não um id próprio. Isso torna a indexação
/// idempotente (reindexar sobrescreve, nunca duplica) e garante que todo vetor
/// aponte de volta para o fato exato que o originou.
/// </remarks>
internal sealed class ItemNarrativeRecord
{
    public Guid LedgerEntryId { get; set; }

    public Guid ItemId { get; set; }

    public string Sku { get; set; } = string.Empty;

    public DateTimeOffset OccurredAt { get; set; }

    /// <summary>O texto que foi embedado. Guardado junto para a busca poder mostrar a evidência.</summary>
    public string Text { get; set; } = string.Empty;

    public Vector Embedding { get; set; } = null!;
}
