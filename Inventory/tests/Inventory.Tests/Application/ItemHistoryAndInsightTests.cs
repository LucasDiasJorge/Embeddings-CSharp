using Inventory.Application.Contracts.Insights;
using Inventory.Application.Contracts.Items;
using Inventory.Domain.Items;
using Inventory.Tests.Fakes;

namespace Inventory.Tests.Application;

public class ItemHistoryAndInsightTests
{
    [Fact]
    public async Task Historico_pelo_id_narra_cada_evento_com_os_nomes_resolvidos()
    {
        var app = new ApplicationHarness();
        var almoxarifado = await app.GivenLocationAsync("ALM-A3", "Almoxarifado Central A3");
        var sala = await app.GivenLocationAsync("SALA-204", "Sala 204 — TI");
        var item = await app.GivenItemAsync("PAT-001", "Notebook Dell Latitude", almoxarifado);

        await app.ItemService.MoveAsync(item, new MoveItemCommand(
            sala.Value, "lucas", MovementReason.Transfer, "empréstimo para o time de TI"));

        var history = await app.ItemService.GetHistoryAsync(item);

        Assert.True(history.IsSuccess);
        Assert.Equal(2, history.Value.Entries.Count);

        var movimento = history.Value.Entries[1];
        Assert.Equal("Almoxarifado Central A3 (ALM-A3)", movimento.FromLocation);
        Assert.Equal("Sala 204 — TI (SALA-204)", movimento.ToLocation);

        // A narrativa precisa carregar nome, etiqueta, os dois lugares e o motivo:
        // é literalmente o texto que vai virar vetor.
        Assert.Contains("Notebook Dell Latitude", movimento.Narrative);
        Assert.Contains("PAT-001", movimento.Narrative);
        Assert.Contains("Almoxarifado Central A3", movimento.Narrative);
        Assert.Contains("Sala 204 — TI", movimento.Narrative);
        Assert.Contains("transferência operacional", movimento.Narrative);
        Assert.Contains("empréstimo para o time de TI", movimento.Narrative);
    }

    [Fact]
    public async Task Movimentacao_sem_motivo_declarado_e_transferencia_operacional()
    {
        var app = new ApplicationHarness();
        var almoxarifado = await app.GivenLocationAsync("ALM-A3", "Almoxarifado Central A3");
        var sala = await app.GivenLocationAsync("SALA-204", "Sala 204 — TI");
        var item = await app.GivenItemAsync("PAT-001", "Notebook Dell Latitude", almoxarifado);

        // O coletor manda só o destino; o motivo é opcional no corpo da requisição.
        await app.ItemService.MoveAsync(item, new MoveItemCommand(sala.Value, "lucas", null, null));

        var movimento = Assert.Single(
            await app.Ledger.ListByItemAsync(item), entry => entry.Kind == LedgerEntryKind.Moved);

        // Registration ficaria à frente na enum; assumir o zero silenciosamente mentiria no razão.
        Assert.Equal(MovementReason.Transfer, movimento.Reason);
    }

    [Fact]
    public async Task Insights_contam_o_que_o_razao_ja_sabe_sem_nenhum_embedding()
    {
        var app = new ApplicationHarness(semanticIndexEnabled: false);
        var almoxarifado = await app.GivenLocationAsync("ALM-A3", "Almoxarifado Central A3");
        var sala = await app.GivenLocationAsync("SALA-204", "Sala 204 — TI");
        var item = await app.GivenItemAsync("PAT-001", "Notebook Dell Latitude", almoxarifado);

        await app.ItemService.MoveAsync(item, new MoveItemCommand(
            sala.Value, "lucas", MovementReason.Transfer, null));

        app.Clock.Advance(TimeSpan.FromDays(120));

        var insights = await app.InsightService.GetInsightsAsync(item);

        Assert.True(insights.IsSuccess);
        Assert.Equal(1, insights.Value.TotalMovements);
        Assert.Equal(0, insights.Value.TimesCounted);
        Assert.Equal(120, insights.Value.DaysSinceLastSeen);
        Assert.Equal("Sala 204 — TI (SALA-204)", insights.Value.CurrentLocation);
        Assert.Contains(insights.Value.Highlights, h => h.Contains("Sem confirmação física há 120 dias"));
        Assert.Contains(insights.Value.Highlights, h => h.Contains("Nunca passou por uma contagem"));
    }

    [Fact]
    public async Task Sem_indice_semantico_a_busca_por_prompt_falha_explicitamente()
    {
        var app = new ApplicationHarness(semanticIndexEnabled: false);

        var result = await app.InsightService.SearchItemsAsync(
            new ItemPromptSearchQuery("notebooks que vivem sumindo"));

        // Lista vazia seria pior que erro: passaria por "não há nada assim no acervo".
        Assert.False(result.IsSuccess);
        Assert.Equal("insight.index_disabled", result.Error.Code);
    }

    [Fact]
    public async Task Toda_movimentacao_alimenta_o_indice_semantico()
    {
        var app = new ApplicationHarness();
        var almoxarifado = await app.GivenLocationAsync("ALM-A3", "Almoxarifado Central A3");
        var sala = await app.GivenLocationAsync("SALA-204", "Sala 204 — TI");
        var item = await app.GivenItemAsync("PAT-001", "Notebook Dell Latitude", almoxarifado);

        await app.ItemService.MoveAsync(item, new MoveItemCommand(
            sala.Value, "lucas", MovementReason.Transfer, null));

        Assert.Equal(2, app.Index.Indexed.Count);
        Assert.All(app.Index.Indexed, narrative => Assert.Equal("PAT-001", narrative.Sku));
    }

    [Fact]
    public async Task Busca_por_prompt_devolve_itens_ranqueados_com_a_evidencia()
    {
        var app = new ApplicationHarness();
        var almoxarifado = await app.GivenLocationAsync("ALM-A3", "Almoxarifado Central A3");
        var sala = await app.GivenLocationAsync("SALA-204", "Sala 204 — TI");

        var notebook = await app.GivenItemAsync("PAT-001", "Notebook Dell Latitude", almoxarifado);
        await app.GivenItemAsync("PAT-002", "Cadeira de escritório", almoxarifado);

        await app.ItemService.MoveAsync(notebook, new MoveItemCommand(
            sala.Value, "lucas", MovementReason.Transfer, null));

        var result = await app.InsightService.SearchItemsAsync(new ItemPromptSearchQuery("Notebook Dell"));

        Assert.True(result.IsSuccess);

        var hit = Assert.Single(result.Value);
        Assert.Equal("PAT-001", hit.Sku);
        Assert.Equal("Sala 204 — TI (SALA-204)", hit.CurrentLocation);

        // Dois eventos do mesmo item casaram e vieram agregados num único resultado,
        // com as narrativas anexadas para o usuário conferir por que ele apareceu.
        Assert.Equal(2, hit.MatchCount);
        Assert.Equal(2, hit.Evidence.Count);
    }

    [Fact]
    public async Task Reindexar_reconstroi_o_indice_a_partir_do_razao()
    {
        var app = new ApplicationHarness();
        var almoxarifado = await app.GivenLocationAsync("ALM-A3", "Almoxarifado Central A3");
        await app.GivenItemAsync("PAT-001", "Notebook Dell Latitude", almoxarifado);
        await app.GivenItemAsync("PAT-002", "Cadeira de escritório", almoxarifado);

        // Simula o cenário real de quem ligou o embedding depois de já ter histórico.
        app.Index.Indexed.Clear();

        var report = await app.InsightService.ReindexAsync();

        Assert.True(report.IsSuccess);
        Assert.Equal(2, report.Value.IndexedEntries);
        Assert.Equal(app.Ledger.Entries.Count, app.Index.Indexed.Count);
    }

    [Fact]
    public async Task Reindexar_duas_vezes_nao_duplica_vetor()
    {
        var app = new ApplicationHarness();
        var almoxarifado = await app.GivenLocationAsync("ALM-A3", "Almoxarifado Central A3");
        await app.GivenItemAsync("PAT-001", "Notebook Dell Latitude", almoxarifado);

        await app.InsightService.ReindexAsync();
        await app.InsightService.ReindexAsync();

        // A chave do vetor é o id da linha do razão, então reindexar sobrescreve.
        Assert.Equal(app.Ledger.Entries.Count, app.Index.Indexed.Count);
    }
}
