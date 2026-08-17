using System.Globalization;
using System.Text;
using Inventory.Application.Contracts.Insights;
using Inventory.Domain.Items;
using Inventory.Domain.Locations;

namespace Inventory.Application.Narration;

/// <summary>
/// Traduz fatos do razão para português corrido.
/// </summary>
/// <remarks>
/// Este é o componente mais importante para o futuro do embedding, e o motivo é simples:
/// um vetor só é tão bom quanto o texto que o originou. <c>Moved(a1b2…, c3d4…)</c> não
/// diz nada a um modelo de linguagem; "movido do Almoxarifado Central A3 para a Sala 204 —
/// TI, motivo: acerto de inventário" diz tudo. Por isso o narrador resolve os nomes das
/// localizações, escreve o motivo por extenso e mantém o SKU e a data no texto — são esses
/// termos que a pessoa vai usar quando perguntar em linguagem natural.
/// </remarks>
public sealed class ItemNarrator
{
    private static readonly CultureInfo Culture = new("pt-BR");

    public ItemNarrative ToNarrative(
        ItemLedgerEntry entry, Item item, IReadOnlyDictionary<LocationId, Location> locations) =>
        new(entry.Id, entry.ItemId, item.Sku.Value, entry.OccurredAt, Narrate(entry, item, locations));

    public string Narrate(ItemLedgerEntry entry, Item item, IReadOnlyDictionary<LocationId, Location> locations)
    {
        var text = new StringBuilder()
            .Append(FormatMoment(entry.OccurredAt))
            .Append(" — ")
            .Append(item.Name)
            .Append(" (etiqueta ")
            .Append(item.Sku.Value)
            .Append(") ")
            .Append(DescribeEvent(entry, locations))
            .Append(" Responsável: ")
            .Append(entry.Actor)
            .Append('.');

        if (entry.InventoryCountId is { } countId)
        {
            text.Append(" Contagem de inventário ").Append(countId.Value).Append('.');
        }

        if (!string.IsNullOrWhiteSpace(entry.Note))
        {
            text.Append(" Observação: ").Append(entry.Note.TrimEnd('.')).Append('.');
        }

        return text.ToString();
    }

    private static string DescribeEvent(ItemLedgerEntry entry, IReadOnlyDictionary<LocationId, Location> locations)
    {
        var from = Describe(entry.FromLocationId, locations);
        var to = Describe(entry.ToLocationId, locations);

        return entry.Kind switch
        {
            LedgerEntryKind.Registered =>
                $"foi cadastrado no acervo e alocado em {to}.",

            LedgerEntryKind.Moved =>
                $"foi movido de {from} para {to}. Motivo: {DescribeReason(entry.Reason)}.",

            LedgerEntryKind.CountConfirmed =>
                $"foi conferido em {to} e estava exatamente onde o sistema esperava.",

            LedgerEntryKind.CountMissing =>
                $"era esperado em {from} e não foi encontrado; passou a constar como faltante.",

            LedgerEntryKind.Renamed =>
                "teve os dados de cadastro atualizados.",

            LedgerEntryKind.Retired =>
                $"recebeu baixa definitiva, saindo de {from}.",

            _ => "sofreu uma alteração de estado."
        };
    }

    private static string DescribeReason(MovementReason? reason) => reason switch
    {
        MovementReason.Registration => "entrada inicial no acervo",
        MovementReason.Transfer => "transferência operacional",
        MovementReason.CountReconciliation => "acerto de inventário (item encontrado em local divergente)",
        MovementReason.Correction => "correção de lançamento anterior",
        _ => "não informado"
    };

    private static string Describe(LocationId? id, IReadOnlyDictionary<LocationId, Location> locations)
    {
        if (id is not { } locationId)
        {
            return "nenhuma localização";
        }

        return locations.TryGetValue(locationId, out var location)
            ? $"{location.Name} ({location.Code.Value})"
            : $"localização {locationId.Value}";
    }

    private static string FormatMoment(DateTimeOffset moment) =>
        moment.ToString("dd/MM/yyyy 'às' HH:mm", Culture);
}
