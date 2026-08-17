using Inventory.Domain.Counting;
using Inventory.Domain.Items;

namespace Inventory.Application.Ports.Driven;

/// <summary>
/// O razão é append-only por contrato: não há Update nem Delete nesta porta,
/// e essa ausência é a garantia de que o histórico não pode ser reescrito.
/// </summary>
public interface IItemLedgerRepository
{
    Task AppendAsync(IReadOnlyCollection<ItemLedgerEntry> entries, CancellationToken cancellationToken = default);

    /// <summary>A vida do item em ordem cronológica, do cadastro até hoje.</summary>
    Task<IReadOnlyList<ItemLedgerEntry>> ListByItemAsync(
        ItemId itemId, CancellationToken cancellationToken = default);

    /// <summary>Tudo que uma contagem específica provocou — o anexo do relatório de auditoria.</summary>
    Task<IReadOnlyList<ItemLedgerEntry>> ListByCountAsync(
        InventoryCountId inventoryCountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Varredura paginada do razão inteiro, em ordem estável. Existe para a reindexação
    /// semântica poder reconstruir o índice a partir do zero sem carregar tudo na memória.
    /// </summary>
    Task<IReadOnlyList<ItemLedgerEntry>> ListPageAsync(
        int skip, int take, CancellationToken cancellationToken = default);
}
