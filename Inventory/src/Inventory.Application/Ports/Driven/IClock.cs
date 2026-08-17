namespace Inventory.Application.Ports.Driven;

/// <summary>
/// O tempo como porta. Parece exagero até você precisar testar
/// "item não é visto há 90 dias" sem esperar 90 dias.
/// </summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
