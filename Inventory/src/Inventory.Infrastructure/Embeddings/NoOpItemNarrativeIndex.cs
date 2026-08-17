using Inventory.Application.Contracts.Insights;
using Inventory.Application.Ports.Driven;
using Inventory.Domain.Items;

namespace Inventory.Infrastructure.Embeddings;

/// <summary>
/// Adaptador padrão do índice semântico: não faz nada, e diz isso abertamente.
/// </summary>
/// <remarks>
/// Existe para que a API rode sem Ollama, sem pgvector e sem GPU — o inventário
/// funciona inteiro sem embedding nenhum. Quando você for plugar o índice de verdade,
/// o trabalho é escrever uma classe irmã desta:
/// <list type="number">
///   <item>Gere o vetor de <c>ItemNarrative.Text</c> (o texto já vem pronto do <c>ItemNarrator</c>).</item>
///   <item>Grave em <c>item_narratives(ledger_entry_id, item_id, text, embedding vector(N))</c>.</item>
///   <item>Em <c>SearchAsync</c>, embede a pergunta e ordene por distância de cosseno,
///         filtrando por <c>item_id</c> quando houver escopo.</item>
///   <item>Troque o registro em <c>AddInfrastructure</c>. Nada mais no projeto muda.</item>
/// </list>
/// Como as narrativas são derivadas do razão, dá para indexar retroativamente todo o
/// histórico já acumulado no dia em que isso acontecer.
/// </remarks>
internal sealed class NoOpItemNarrativeIndex : IItemNarrativeIndex
{
    public bool IsEnabled => false;

    public Task IndexAsync(
        IReadOnlyCollection<ItemNarrative> narratives, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<IReadOnlyList<NarrativeMatch>> SearchAsync(
        string query, ItemId? scope, int top, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<NarrativeMatch>>([]);
}
