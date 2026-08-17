using Inventory.Application.Ports.Driven;
using Inventory.Infrastructure.Embeddings;
using Inventory.Infrastructure.Persistence;
using Inventory.Infrastructure.Persistence.Repositories;
using Inventory.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OllamaSharp;

namespace Inventory.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Pluga os adaptadores concretos nas portas dirigidas.
    /// Este é o único método do projeto que sabe, ao mesmo tempo, o nome de uma
    /// interface do domínio e o nome do Postgres. Trocar de banco começa e termina aqui.
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' não configurada.");

        services.AddDbContext<InventoryDbContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IItemRepository, ItemRepository>();
        services.AddScoped<ILocationRepository, LocationRepository>();
        services.AddScoped<IInventoryCountRepository, InventoryCountRepository>();
        services.AddScoped<IItemLedgerRepository, ItemLedgerRepository>();

        services.AddSingleton<IClock, SystemClock>();
        services.AddSemanticIndex(configuration, connectionString);

        return services;
    }

    /// <summary>
    /// Escolhe o adaptador do índice semântico conforme <c>Embeddings:Enabled</c>.
    /// Desligado, a aplicação inteira continua funcionando — só a busca por prompt
    /// responde 409 dizendo que não está configurada.
    /// </summary>
    private static void AddSemanticIndex(
        this IServiceCollection services, IConfiguration configuration, string inventoryConnectionString)
    {
        var options = configuration.GetSection(EmbeddingOptions.SectionName).Get<EmbeddingOptions>()
            ?? new EmbeddingOptions();

        services.AddSingleton(options);

        if (!options.Enabled)
        {
            services.AddScoped<IItemNarrativeIndex, NoOpItemNarrativeIndex>();
            return;
        }

        services.AddSingleton<IOllamaApiClient>(
            _ => new OllamaApiClient(options.Endpoint, options.Model));

        services.AddSingleton<OllamaEmbeddingGenerator>();

        services.AddDbContext<NarrativeDbContext>(builder =>
            builder.UseNpgsql(
                options.ConnectionString ?? inventoryConnectionString,
                npgsql => npgsql.UseVector()));

        services.AddScoped<IItemNarrativeIndex, PgVectorItemNarrativeIndex>();
    }

    /// <summary>
    /// Cria o schema se ele ainda não existir. Conveniência de desenvolvimento —
    /// em produção use <c>sql/schema.sql</c> ou migrations.
    /// </summary>
    public static async Task EnsureSchemaAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();

        await scope.ServiceProvider.GetRequiredService<InventoryDbContext>()
            .Database.EnsureCreatedAsync(cancellationToken);

        // O contexto de narrativas só existe quando o embedding está ligado.
        if (scope.ServiceProvider.GetService<NarrativeDbContext>() is { } narratives)
        {
            await EnsureNarrativeSchemaAsync(narratives, cancellationToken);
        }
    }

    /// <summary>
    /// EnsureCreated não resolve o índice semântico: no arranjo normal ele divide o banco
    /// com o inventário, que acabou de ser criado acima — e EnsureCreated só olha se o
    /// <b>banco</b> existe, não as tabelas. Sozinho, ele deixaria item_narratives por criar.
    /// </summary>
    private static async Task EnsureNarrativeSchemaAsync(
        NarrativeDbContext narratives, CancellationToken cancellationToken)
    {
        var creator = narratives.GetService<IRelationalDatabaseCreator>();

        if (!await creator.ExistsAsync(cancellationToken))
        {
            await creator.CreateAsync(cancellationToken);
        }

        var alreadyThere = await narratives.Database
            .SqlQueryRaw<bool>("SELECT to_regclass('public.item_narratives') IS NOT NULL AS \"Value\"")
            .SingleAsync(cancellationToken);

        if (!alreadyThere)
        {
            await creator.CreateTablesAsync(cancellationToken);
        }
    }
}
