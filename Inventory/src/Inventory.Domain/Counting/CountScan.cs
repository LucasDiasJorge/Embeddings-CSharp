using Inventory.Domain.Items;

namespace Inventory.Domain.Counting;

/// <summary>Uma leitura do auditor: "eu vi este item, aqui, agora".</summary>
public sealed record CountScan(ItemId ItemId, DateTimeOffset ScannedAt);
