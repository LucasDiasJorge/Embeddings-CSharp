namespace Inventory.Application.Ports.Driven;

/// <summary>
/// Fecha a transação. Os repositórios só marcam intenção; nada é gravado até aqui.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
