namespace Inventory.Domain.Items;

public enum ItemStatus
{
    /// <summary>Presença confirmada na localização registrada.</summary>
    Active = 0,

    /// <summary>Era esperado numa contagem e não foi encontrado. <c>LocationId</c> vira "última localização conhecida".</summary>
    Missing = 1,

    /// <summary>Baixado: saiu do controle em definitivo (descarte, venda, perda assumida). Não movimenta mais.</summary>
    Retired = 2
}
