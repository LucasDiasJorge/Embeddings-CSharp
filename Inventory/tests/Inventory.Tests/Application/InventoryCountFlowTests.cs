using Inventory.Application.Common;
using Inventory.Application.Contracts.Counting;
using Inventory.Application.Contracts.Items;
using Inventory.Domain.Items;
using Inventory.Tests.Fakes;

namespace Inventory.Tests.Application;

/// <summary>
/// O ciclo completo do auditor, ponta a ponta, sem infraestrutura nenhuma.
/// </summary>
public class InventoryCountFlowTests
{
    [Fact]
    public async Task Fechar_a_contagem_aplica_o_veredito_a_cada_item()
    {
        var app = new ApplicationHarness();
        var almoxarifado = await app.GivenLocationAsync("ALM-A3", "Almoxarifado Central A3");
        var sala = await app.GivenLocationAsync("SALA-204", "Sala 204 — TI");

        var presente = await app.GivenItemAsync("PAT-001", "Notebook Dell Latitude", almoxarifado);
        var sumido = await app.GivenItemAsync("PAT-002", "Monitor LG 27", almoxarifado);
        var intruso = await app.GivenItemAsync("PAT-003", "Projetor Epson", sala);

        var count = await app.OpenCountAsync(almoxarifado, "Ana, auditoria");

        await app.ScanAsync(count, "PAT-001");
        await app.ScanAsync(count, "PAT-003"); // apareceu numa prateleira onde não devia estar

        var report = await app.CountService.CloseAsync(count);

        Assert.True(report.IsSuccess);
        Assert.Equal(0.5, report.Value.Accuracy);
        Assert.False(report.Value.IsClean);

        Assert.Equal("PAT-001", Assert.Single(report.Value.Confirmed).Sku);
        Assert.Equal("PAT-002", Assert.Single(report.Value.Missing).Sku);
        Assert.Equal("PAT-003", Assert.Single(report.Value.Relocated).Sku);

        Assert.Equal(ItemStatus.Active, (await app.LoadAsync(presente)).Status);
        Assert.Equal(ItemStatus.Missing, (await app.LoadAsync(sumido)).Status);

        // O físico venceu o sistema: o projetor agora mora onde ele realmente está.
        Assert.Equal(almoxarifado, (await app.LoadAsync(intruso)).LocationId);
    }

    [Fact]
    public async Task Cada_consequencia_do_fechamento_vira_uma_linha_no_razao()
    {
        var app = new ApplicationHarness();
        var almoxarifado = await app.GivenLocationAsync("ALM-A3", "Almoxarifado Central A3");
        var item = await app.GivenItemAsync("PAT-001", "Notebook Dell Latitude", almoxarifado);

        var count = await app.OpenCountAsync(almoxarifado, "Ana, auditoria");
        await app.ScanAsync(count, "PAT-001");
        await app.CountService.CloseAsync(count);

        var razao = await app.Ledger.ListByItemAsync(item);

        Assert.Collection(razao,
            first => Assert.Equal(LedgerEntryKind.Registered, first.Kind),
            second =>
            {
                Assert.Equal(LedgerEntryKind.CountConfirmed, second.Kind);
                Assert.Equal(count, second.InventoryCountId);
                Assert.Equal("Ana, auditoria", second.Actor);
            });

        // Sequência sem buracos: é o que garante a ordem da linha do tempo.
        Assert.Equal([1, 2], razao.Select(entry => entry.Sequence));
    }

    [Fact]
    public async Task Duas_contagens_abertas_na_mesma_localizacao_sao_recusadas()
    {
        var app = new ApplicationHarness();
        var almoxarifado = await app.GivenLocationAsync("ALM-A3", "Almoxarifado Central A3");
        await app.OpenCountAsync(almoxarifado, "Ana");

        var segunda = await app.CountService.OpenAsync(
            new OpenInventoryCountCommand(almoxarifado.Value, "Bruno"));

        Assert.False(segunda.IsSuccess);
        Assert.Equal(ErrorType.Conflict, segunda.Error.Type);
        Assert.Equal("count.already_open", segunda.Error.Code);
    }

    [Fact]
    public async Task Movimentacao_durante_a_contagem_nao_altera_o_que_foi_congelado()
    {
        var app = new ApplicationHarness();
        var almoxarifado = await app.GivenLocationAsync("ALM-A3", "Almoxarifado Central A3");
        var sala = await app.GivenLocationAsync("SALA-204", "Sala 204 — TI");
        await app.GivenItemAsync("PAT-001", "Notebook Dell Latitude", almoxarifado);

        var count = await app.OpenCountAsync(almoxarifado, "Ana");

        // Alguém movimenta o item pelas costas do auditor, com a contagem já aberta.
        var item = await app.ItemService.GetBySkuAsync("PAT-001");
        await app.ItemService.MoveAsync(
            new ItemId(item.Value.Id),
            new MoveItemCommand(sala.Value, "lucas", MovementReason.Transfer, null));

        var report = await app.CountService.CloseAsync(count);

        // O auditor ainda é cobrado pelo que estava lá quando ele começou — é isso
        // que torna a contagem um documento auditável e não uma foto embaçada.
        Assert.True(report.IsSuccess);
        Assert.Equal("PAT-001", Assert.Single(report.Value.Missing).Sku);
    }

    [Fact]
    public async Task Contagem_cancelada_nao_toca_em_item_nenhum()
    {
        var app = new ApplicationHarness();
        var almoxarifado = await app.GivenLocationAsync("ALM-A3", "Almoxarifado Central A3");
        var item = await app.GivenItemAsync("PAT-001", "Notebook Dell Latitude", almoxarifado);

        var count = await app.OpenCountAsync(almoxarifado, "Ana");
        await app.CountService.CancelAsync(count, new CancelInventoryCountCommand("prateleira interditada"));

        Assert.Equal(ItemStatus.Active, (await app.LoadAsync(item)).Status);
        Assert.Single(await app.Ledger.ListByItemAsync(item)); // só o cadastro
    }

    [Fact]
    public async Task Localizacao_com_itens_nao_pode_ser_desativada()
    {
        var app = new ApplicationHarness();
        var almoxarifado = await app.GivenLocationAsync("ALM-A3", "Almoxarifado Central A3");
        await app.GivenItemAsync("PAT-001", "Notebook Dell Latitude", almoxarifado);

        var result = await app.LocationService.DeactivateAsync(almoxarifado);

        Assert.False(result.IsSuccess);
        Assert.Equal("location.not_empty", result.Error.Code);
    }
}
