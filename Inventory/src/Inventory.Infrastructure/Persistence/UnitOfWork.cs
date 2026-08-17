using Inventory.Application.Ports.Driven;

namespace Inventory.Infrastructure.Persistence;

internal sealed class UnitOfWork(InventoryDbContext context) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);
}
