using Inventory.Application.Narration;
using Inventory.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registra o miolo: os casos de uso e seus colaboradores.
    /// As portas dirigidas (repositórios, relógio, índice) ficam por conta de quem
    /// escolher os adaptadores — normalmente <c>AddInfrastructure</c>, mas num teste
    /// pode ser um punhado de fakes em memória.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<ItemNarrator>();
        services.AddScoped<LedgerCommitter>();

        services.AddScoped<LocationService>();
        services.AddScoped<ItemService>();
        services.AddScoped<InventoryCountService>();
        services.AddScoped<ItemInsightService>();

        return services;
    }
}
