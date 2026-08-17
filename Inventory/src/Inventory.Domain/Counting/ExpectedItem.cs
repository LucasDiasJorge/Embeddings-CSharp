using Inventory.Domain.Items;

namespace Inventory.Domain.Counting;

/// <summary>
/// Um item que, no instante da abertura, o sistema acreditava estar na localização auditada.
/// Congelar isso é o que torna a contagem auditável: o resultado não muda se alguém
/// movimentar itens enquanto o auditor caminha pelo galpão.
/// </summary>
public sealed record ExpectedItem(ItemId ItemId);
