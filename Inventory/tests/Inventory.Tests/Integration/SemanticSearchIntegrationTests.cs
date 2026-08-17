using Inventory.Application;
using Inventory.Application.Contracts.Counting;
using Inventory.Application.Contracts.Insights;
using Inventory.Application.Contracts.Items;
using Inventory.Application.Contracts.Locations;
using Inventory.Application.Services;
using Inventory.Domain.Counting;
using Inventory.Domain.Items;
using Inventory.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit.Abstractions;

namespace Inventory.Tests.Integration;

/// <summary>
/// O caminho completo com as peças de verdade: Postgres, EF Core, Ollama e pgvector.
/// </summary>
/// <remarks>
/// Os testes de domínio e de caso de uso rodam contra fakes e não provam que um vetor
/// chega ao banco. Este prova — e prova a parte que mais importa: que a busca acha por
/// <b>sentido</b>, não por palavra igual.
/// <para>
/// Se Postgres ou Ollama não estiverem no ar, o teste sai sem falhar e diz o porquê na
/// saída, para que <c>dotnet test</c> continue verde numa máquina limpa. Rode
/// <c>dotnet test --filter Category=Integration</c> para exercitá-lo de propósito.
/// </para>
/// </remarks>
[Trait("Category", "Integration")]
public class SemanticSearchIntegrationTests(ITestOutputHelper output)
{
    private const string Host = "Host=localhost;Port=5432;Username=root;Password=root";
    private const string TestDatabase = "inventory_integration_test";
    private const string OllamaEndpoint = "http://localhost:11434";
    private const string Model = "qwen3-embedding:4b";

    [Fact]
    public async Task Busca_por_prompt_encontra_o_item_certo_pelo_sentido_do_historico()
    {
        if (await ProbeAsync() is { } motivo)
        {
            output.WriteLine($"Ignorado: {motivo}");
            return;
        }

        await RecreateDatabaseAsync();

        try
        {
            await using var provider = BuildProvider();
            await provider.EnsureSchemaAsync();

            await using var scope = provider.CreateAsyncScope();
            var locations = scope.ServiceProvider.GetRequiredService<LocationService>();
            var items = scope.ServiceProvider.GetRequiredService<ItemService>();
            var counts = scope.ServiceProvider.GetRequiredService<InventoryCountService>();
            var insights = scope.ServiceProvider.GetRequiredService<ItemInsightService>();

            // --- cenário -----------------------------------------------------------
            var almoxarifado = await Ok(locations.CreateAsync(
                new CreateLocationCommand("ALM-A3", "Almoxarifado Central A3", null)));

            var sala = await Ok(locations.CreateAsync(
                new CreateLocationCommand("SALA-204", "Sala 204 — TI", null)));

            var notebook = await Ok(items.RegisterAsync(new RegisterItemCommand(
                "PAT-001", "Notebook Dell Latitude 5440", "Máquina de desenvolvimento",
                almoxarifado.Id, "lucas")));

            var cadeira = await Ok(items.RegisterAsync(new RegisterItemCommand(
                "PAT-002", "Cadeira de escritório ergonômica", null, almoxarifado.Id, "lucas")));

            await Ok(items.MoveAsync(
                new ItemId(cadeira.Id),
                new MoveItemCommand(sala.Id, "lucas", MovementReason.Transfer, "troca de andar")));

            // --- auditoria: o notebook era esperado e não apareceu -----------------
            var count = await Ok(counts.OpenAsync(new OpenInventoryCountCommand(almoxarifado.Id, "Ana, auditoria")));
            var report = await Ok(counts.CloseAsync(new InventoryCountId(count.Id)));

            Assert.Equal(0d, report.Accuracy);
            Assert.Equal("PAT-001", Assert.Single(report.Missing).Sku);

            var estado = await Ok(items.GetAsync(new ItemId(notebook.Id)));
            Assert.Equal(nameof(ItemStatus.Missing), estado.Status);

            // --- a prova: pergunta sem NENHUMA palavra em comum com a narrativa ----
            var hits = await Ok(insights.SearchItemsAsync(
                new ItemPromptSearchQuery("qual equipamento de informática desapareceu do estoque?", 2)));

            Assert.NotEmpty(hits);
            output.WriteLine("Ranking devolvido:");
            foreach (var hit in hits)
            {
                output.WriteLine($"  {hit.Score:F3}  {hit.Sku}  {hit.Name}  ({hit.MatchCount} evento(s))");
            }

            // "equipamento de informática" não aparece em lugar nenhum do razão; "desapareceu"
            // tampouco — a narrativa diz "não foi encontrado". Se o notebook vier na frente
            // da cadeira, foi o embedding trabalhando, não substring.
            Assert.Equal("PAT-001", hits[0].Sku);
            Assert.NotEmpty(hits[0].Evidence);

            output.WriteLine($"Evidência: {hits[0].Evidence[0].Text}");
        }
        finally
        {
            await DropDatabaseAsync();
        }
    }

    private static ServiceProvider BuildProvider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = $"{Host};Database={TestDatabase}",
                ["Embeddings:Enabled"] = "true",
                ["Embeddings:Endpoint"] = OllamaEndpoint,
                ["Embeddings:Model"] = Model,
                ["Embeddings:Dimensions"] = "2560"
            })
            .Build();

        return new ServiceCollection()
            .AddApplication()
            .AddInfrastructure(configuration)
            .BuildServiceProvider();
    }

    /// <summary>Devolve o motivo de pular, ou null quando a infraestrutura está disponível.</summary>
    private static async Task<string?> ProbeAsync()
    {
        try
        {
            await using var connection = new NpgsqlConnection($"{Host};Database=postgres");
            await connection.OpenAsync();
        }
        catch (Exception error)
        {
            return $"Postgres indisponível em localhost:5432 ({error.GetType().Name}).";
        }

        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var tags = await http.GetStringAsync($"{OllamaEndpoint}/api/tags");

            return tags.Contains(Model, StringComparison.OrdinalIgnoreCase)
                ? null
                : $"Modelo {Model} não está baixado no Ollama (ollama pull {Model}).";
        }
        catch (Exception error)
        {
            return $"Ollama indisponível em {OllamaEndpoint} ({error.GetType().Name}).";
        }
    }

    private static async Task RecreateDatabaseAsync()
    {
        await DropDatabaseAsync();

        await using var connection = new NpgsqlConnection($"{Host};Database=postgres");
        await connection.OpenAsync();
        await using var create = new NpgsqlCommand($"CREATE DATABASE {TestDatabase}", connection);
        await create.ExecuteNonQueryAsync();
    }

    private static async Task DropDatabaseAsync()
    {
        NpgsqlConnection.ClearAllPools();

        await using var connection = new NpgsqlConnection($"{Host};Database=postgres");
        await connection.OpenAsync();
        await using var drop = new NpgsqlCommand(
            $"DROP DATABASE IF EXISTS {TestDatabase} WITH (FORCE)", connection);
        await drop.ExecuteNonQueryAsync();
    }

    private static async Task<T> Ok<T>(Task<Inventory.Application.Common.Result<T>> pending)
    {
        var result = await pending;

        Assert.True(result.IsSuccess, result.IsSuccess ? "" : $"{result.Error.Code}: {result.Error.Message}");

        return result.Value;
    }
}
