namespace Inventory.Domain.Items;

/// <summary>Por que o item mudou de localização. É o "porquê" que dá valor semântico ao histórico.</summary>
public enum MovementReason
{
    /// <summary>Primeira entrada do item no sistema.</summary>
    Registration = 0,

    /// <summary>Movimentação operacional deliberada.</summary>
    Transfer = 1,

    /// <summary>Acerto automático do fechamento de uma contagem: o item apareceu onde o sistema não esperava.</summary>
    CountReconciliation = 2,

    /// <summary>Correção manual de um lançamento errado.</summary>
    Correction = 3
}
