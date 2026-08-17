using OllamaSharp;
using OllamaSharp.Models;
using Pgvector;

namespace Inventory.Infrastructure.Embeddings;

/// <summary>
/// Converte texto em vetor via Ollama.
/// </summary>
/// <remarks>
/// Trata pergunta e documento de formas diferentes de propósito. Embedding assimétrico
/// é o que faz uma pergunta curta ("o que sumiu do almoxarifado?") encontrar um parágrafo
/// longo de histórico: a pergunta ganha uma instrução que descreve a tarefa, o documento vai cru.
/// Embedar os dois do mesmo jeito é o erro mais comum e derruba o recall de forma silenciosa.
/// </remarks>
internal sealed class OllamaEmbeddingGenerator(IOllamaApiClient ollama, EmbeddingOptions options)
{
    public Task<Vector> EmbedQueryAsync(string query, CancellationToken cancellationToken) =>
        EmbedOneAsync($"Instruct: {options.QueryInstruction}\nQuery: {query}", cancellationToken);

    public async Task<IReadOnlyList<Vector>> EmbedDocumentsAsync(
        IReadOnlyList<string> documents, CancellationToken cancellationToken)
    {
        var vectors = new List<Vector>(documents.Count);

        foreach (var batch in documents.Chunk(Math.Max(options.BatchSize, 1)))
        {
            vectors.AddRange(await EmbedBatchAsync(batch, cancellationToken));
        }

        return vectors;
    }

    private async Task<Vector> EmbedOneAsync(string text, CancellationToken cancellationToken)
    {
        var vectors = await EmbedBatchAsync([text], cancellationToken);

        return vectors[0];
    }

    private async Task<IReadOnlyList<Vector>> EmbedBatchAsync(
        string[] batch, CancellationToken cancellationToken)
    {
        var response = await ollama.EmbedAsync(
            new EmbedRequest
            {
                Model = options.Model,
                Input = [.. batch],
                Dimensions = options.TruncateDimensions ? options.Dimensions : null
            },
            cancellationToken);

        if (response.Embeddings is not { Count: > 0 } embeddings || embeddings.Count != batch.Length)
        {
            throw new InvalidOperationException(
                $"O modelo '{options.Model}' devolveu {response.Embeddings?.Count ?? 0} vetores " +
                $"para {batch.Length} textos. Verifique se o modelo está baixado (ollama pull {options.Model}).");
        }

        // A coluna do Postgres tem dimensão fixa: deixar passar um vetor de tamanho errado
        // só adiaria o erro para o INSERT, com uma mensagem muito pior.
        if (embeddings[0].Length != options.Dimensions)
        {
            throw new InvalidOperationException(
                $"O modelo '{options.Model}' devolveu vetores de {embeddings[0].Length} dimensões, " +
                $"mas Embeddings:Dimensions está em {options.Dimensions}. " +
                (options.TruncateDimensions
                    ? "Embeddings:TruncateDimensions está ligado, então ou este modelo não suporta " +
                      "Matryoshka, ou esta versão do Ollama ignora o parâmetro 'dimensions'. " +
                      "Desligue a opção e use a dimensão nativa do modelo."
                    : "Ajuste Embeddings:Dimensions para a dimensão nativa do modelo e recrie a " +
                      "tabela item_narratives."));
        }

        return [.. embeddings.Select(embedding => new Vector(embedding))];
    }
}
