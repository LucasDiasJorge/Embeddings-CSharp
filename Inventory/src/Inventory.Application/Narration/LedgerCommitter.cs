using Inventory.Application.Contracts.Insights;
using Inventory.Application.Ports.Driven;
using Inventory.Domain.Items;
using Inventory.Domain.Locations;

namespace Inventory.Application.Narration;

/// <summary>
/// Fecha a unidade de trabalho de qualquer operação que tenha mexido em itens:
/// grava as novas linhas do razão, commita e só então avisa o índice semântico.
/// </summary>
/// <remarks>
/// A ordem importa. Indexar antes do commit deixaria vetores órfãos apontando para
/// eventos que nunca existiram se a transação falhasse. Indexar depois pode, no pior
/// caso, deixar o índice atrasado — e índice atrasado se reconstrói a partir do razão;
/// razão inconsistente, não.
/// </remarks>
public sealed class LedgerCommitter(
    IItemLedgerRepository ledger,
    ILocationRepository locations,
    IItemNarrativeIndex index,
    ItemNarrator narrator,
    IUnitOfWork unitOfWork)
{
    public async Task CommitAsync(IReadOnlyCollection<Item> touchedItems, CancellationToken cancellationToken)
    {
        var entries = touchedItems.SelectMany(item => item.PendingLedgerEntries).ToArray();

        if (entries.Length > 0)
        {
            await ledger.AppendAsync(entries, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (entries.Length == 0)
        {
            return;
        }

        // Narrar custa uma consulta de localizações; só vale a pena se houver quem consuma.
        var narratives = index.IsEnabled
            ? await NarrateAsync(touchedItems, entries, cancellationToken)
            : [];

        foreach (var item in touchedItems)
        {
            item.ClearPendingLedgerEntries();
        }

        if (narratives.Count > 0)
        {
            await index.IndexAsync(narratives, cancellationToken);
        }
    }

    private async Task<IReadOnlyList<ItemNarrative>> NarrateAsync(
        IReadOnlyCollection<Item> touchedItems,
        IReadOnlyCollection<ItemLedgerEntry> entries,
        CancellationToken cancellationToken)
    {
        var referenced = entries
            .SelectMany(entry => new[] { entry.FromLocationId, entry.ToLocationId })
            .OfType<LocationId>()
            .Distinct();

        var resolved = await locations.FindManyAsync(referenced, cancellationToken);
        var byId = touchedItems.ToDictionary(item => item.Id);

        return [.. entries.Select(entry => narrator.ToNarrative(entry, byId[entry.ItemId], resolved))];
    }
}
