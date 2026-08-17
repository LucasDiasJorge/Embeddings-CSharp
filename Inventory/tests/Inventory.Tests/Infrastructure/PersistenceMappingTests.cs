using Inventory.Domain.Counting;
using Inventory.Domain.Items;
using Inventory.Domain.Locations;
using Inventory.Infrastructure.Embeddings;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Inventory.Tests.Infrastructure;

/// <summary>
/// Os fakes provam que a regra está certa; não provam que ela sobrevive a um round-trip
/// no Postgres. Estes testes constroem o modelo do EF de verdade — sem conexão — e pegam
/// justamente a classe de erro que fake nenhum pega: conversor de value object faltando,
/// coleção owned sem backing field, propriedade calculada que o EF tenta mapear.
/// </summary>
public class PersistenceMappingTests
{
    private static InventoryDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<InventoryDbContext>()
            .UseNpgsql("Host=localhost;Database=modelo-nunca-conectado")
            .Options);

    [Fact]
    public void O_modelo_do_inventario_e_valido()
    {
        using var context = CreateContext();

        var script = context.Database.GenerateCreateScript();

        Assert.Contains("CREATE TABLE items", script);
        Assert.Contains("CREATE TABLE item_ledger_entries", script);
        Assert.Contains("CREATE TABLE inventory_count_expected_items", script);
        Assert.Contains("CREATE TABLE inventory_count_scans", script);
    }

    [Fact]
    public void Identificadores_tipados_e_value_objects_chegam_ao_banco_como_primitivos()
    {
        using var context = CreateContext();
        var item = context.Model.FindEntityType(typeof(Item))!;

        Assert.Equal(typeof(Guid), ProviderTypeOf(item, nameof(Item.Id)));
        Assert.Equal(typeof(Guid), ProviderTypeOf(item, nameof(Item.LocationId)));
        Assert.Equal(typeof(string), ProviderTypeOf(item, nameof(Item.Sku)));

        var location = context.Model.FindEntityType(typeof(Location))!;
        Assert.Equal(typeof(string), ProviderTypeOf(location, nameof(Location.Code)));
    }

    /// <summary>
    /// O conversor é registrado por convenção (ConfigureConventions), não por propriedade,
    /// então ele vive no value converter e não em GetProviderClrType.
    /// </summary>
    private static Type? ProviderTypeOf(IEntityType entity, string propertyName) =>
        entity.FindProperty(propertyName)?.GetValueConverter()?.ProviderClrType;

    [Fact]
    public void A_caixa_de_saida_do_razao_nao_e_persistida()
    {
        using var context = CreateContext();
        var item = context.Model.FindEntityType(typeof(Item))!;

        // PendingLedgerEntries é um buffer de unidade de trabalho; persistir isso
        // duplicaria o razão, que já tem tabela própria.
        Assert.Null(item.FindNavigation(nameof(Item.PendingLedgerEntries)));
        Assert.Null(item.FindProperty(nameof(Item.IsCountable)));
    }

    [Fact]
    public void Colecoes_da_contagem_sao_lidas_pelo_campo_e_nao_pela_propriedade()
    {
        using var context = CreateContext();
        var count = context.Model.FindEntityType(typeof(InventoryCount))!;

        // Expected e Scans são IReadOnlyList: sem acesso por campo, o EF não
        // conseguiria materializar o agregado.
        foreach (var name in new[] { nameof(InventoryCount.Expected), nameof(InventoryCount.Scans) })
        {
            var navigation = count.FindNavigation(name);

            Assert.NotNull(navigation);
            Assert.Equal(PropertyAccessMode.Field, navigation.GetPropertyAccessMode());
        }
    }

    [Fact]
    public void So_existe_uma_contagem_aberta_por_localizacao_no_nivel_do_banco()
    {
        using var context = CreateContext();

        var script = context.Database.GenerateCreateScript();

        Assert.Contains(
            "CREATE UNIQUE INDEX ux_inventory_counts_open_per_location ON inventory_counts (location_id) WHERE status = 0",
            script);
    }

    [Fact]
    public void O_razao_nao_admite_duas_linhas_na_mesma_posicao()
    {
        using var context = CreateContext();

        Assert.Contains(
            "CREATE UNIQUE INDEX ux_ledger_item_sequence ON item_ledger_entries (item_id, sequence)",
            context.Database.GenerateCreateScript());
    }

    [Theory]
    [InlineData(768)]
    [InlineData(1024)]
    [InlineData(2560)]
    public void A_coluna_de_vetor_acompanha_a_dimensao_configurada(int dimensions)
    {
        using var context = new NarrativeDbContext(
            new DbContextOptionsBuilder<NarrativeDbContext>()
                .UseNpgsql("Host=localhost;Database=modelo-nunca-conectado", npgsql => npgsql.UseVector())
                .Options,
            new EmbeddingOptions { Dimensions = dimensions });

        var script = context.Database.GenerateCreateScript();

        Assert.Contains($"embedding vector({dimensions})", script);
        Assert.Contains("CREATE EXTENSION IF NOT EXISTS vector", script);
    }
}
