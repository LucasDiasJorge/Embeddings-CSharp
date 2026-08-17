namespace Inventory.Domain.Counting;

public enum InventoryCountStatus
{
    /// <summary>Aberta: o auditor está em campo registrando leituras.</summary>
    Open = 0,

    /// <summary>Fechada: a reconciliação foi calculada e aplicada aos itens. Imutável.</summary>
    Closed = 1,

    /// <summary>Cancelada: descartada sem afetar nenhum item.</summary>
    Cancelled = 2
}
