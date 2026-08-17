using Inventory.Application.Contracts.Insights;
using Inventory.Domain.Items;

namespace Inventory.Application.Ports.Driven;

/// <summary>
/// Porta para o índice semântico do histórico dos itens — o lugar onde os embeddings entram.
/// </summary>
/// <remarks>
/// A aplicação não sabe (nem quer saber) que existe Ollama, pgvector ou distância de cosseno.
/// Ela só entrega narrativas em texto e faz perguntas em texto. Trocar o modelo de embedding,
/// ou o vetorial inteiro, é escrever outro adaptador desta interface — nada acima muda.
/// <para>
/// O adaptador padrão (<c>NoOpItemNarrativeIndex</c>) responde <c>IsEnabled = false</c> e ignora
/// tudo, então a API roda sem Ollama nenhum. Isso não custa histórico: a narrativa é
/// <b>derivada</b> dos fatos do razão, que já estão sendo gravados desde a primeira
/// movimentação. No dia em que você plugar o adaptador vetorial, dá para reprocessar o
/// razão inteiro e indexar retroativamente — nada se perde por ligar o embedding depois.
/// </para>
/// </remarks>
public interface IItemNarrativeIndex
{
    /// <summary>Falso quando nenhum backend vetorial está configurado; os casos de uso degradam graciosamente.</summary>
    bool IsEnabled { get; }

    /// <summary>Indexa novas linhas do razão. Chamado depois do commit, nunca antes.</summary>
    Task IndexAsync(IReadOnlyCollection<ItemNarrative> narratives, CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca semântica no histórico. Com <paramref name="scope"/> preenchido, responde
    /// "o que aconteceu com ESTE item que se parece com isso?"; sem ele, varre o acervo inteiro.
    /// </summary>
    Task<IReadOnlyList<NarrativeMatch>> SearchAsync(
        string query, ItemId? scope, int top, CancellationToken cancellationToken = default);
}
