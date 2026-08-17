using Inventory.Domain.Common;
using Inventory.Domain.Counting;
using Inventory.Domain.Items;
using Inventory.Domain.Locations;

namespace Inventory.Tests.Domain;

public class InventoryCountTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 2, 13, 0, 0, TimeSpan.Zero);
    private static readonly LocationId Almoxarifado = LocationId.New();

    private static readonly ItemId Presente = ItemId.New();
    private static readonly ItemId Sumido = ItemId.New();
    private static readonly ItemId Intruso = ItemId.New();

    private static InventoryCount Given() =>
        InventoryCount.Open(Almoxarifado, "auditor", [Presente, Sumido], Now);

    [Fact]
    public void Abrir_congela_o_que_o_sistema_acredita_estar_no_lugar()
    {
        var count = Given();

        Assert.Equal(InventoryCountStatus.Open, count.Status);
        Assert.Equal(2, count.Expected.Count);
        Assert.Empty(count.Scans);
    }

    [Fact]
    public void Fechar_separa_confirmados_faltantes_e_inesperados()
    {
        var count = Given();
        count.Scan(Presente, Now);
        count.Scan(Intruso, Now);

        var veredito = count.Close(Now.AddHours(1));

        Assert.Equal([Presente], veredito.Confirmed);
        Assert.Equal([Sumido], veredito.Missing);
        Assert.Equal([Intruso], veredito.Unexpected);
        Assert.False(veredito.IsClean);
        Assert.Equal(0.5, veredito.Accuracy);
    }

    [Fact]
    public void Contagem_sem_divergencia_e_limpa_e_tem_acuracidade_total()
    {
        var count = Given();
        count.Scan(Presente, Now);
        count.Scan(Sumido, Now);

        var veredito = count.Close(Now.AddHours(1));

        Assert.True(veredito.IsClean);
        Assert.Equal(1d, veredito.Accuracy);
    }

    [Fact]
    public void Bipar_a_mesma_etiqueta_duas_vezes_nao_duplica_nada()
    {
        var count = Given();

        count.Scan(Presente, Now);
        count.Scan(Presente, Now.AddMinutes(5));

        Assert.Single(count.Scans);
        // A primeira leitura é a que vale: é quando o item foi de fato visto.
        Assert.Equal(Now, count.Scans[0].ScannedAt);
    }

    [Fact]
    public void Contagem_fechada_nao_aceita_mais_leitura()
    {
        var count = Given();
        count.Close(Now);

        Assert.Equal("count.not_open", Assert.Throws<DomainException>(() => count.Scan(Presente, Now)).Code);
        Assert.Equal("count.not_open", Assert.Throws<DomainException>(() => count.Close(Now)).Code);
    }

    [Fact]
    public void Cancelar_encerra_sem_produzir_veredito()
    {
        var count = Given();
        count.Scan(Presente, Now);

        count.Cancel("prateleira interditada", Now.AddMinutes(10));

        Assert.Equal(InventoryCountStatus.Cancelled, count.Status);
        Assert.Equal("prateleira interditada", count.CancellationReason);
    }

    [Fact]
    public void Veredito_de_contagem_fechada_e_sempre_recalculavel()
    {
        var count = Given();
        count.Scan(Presente, Now);
        var noFechamento = count.Close(Now.AddHours(1));

        // Esperados e leituras ficaram gravados, então reemitir o relatório meses
        // depois tem de dar exatamente o mesmo resultado.
        var reemitido = count.Reconcile();

        Assert.Equal(noFechamento.Confirmed, reemitido.Confirmed);
        Assert.Equal(noFechamento.Missing, reemitido.Missing);
        Assert.Equal(noFechamento.Unexpected, reemitido.Unexpected);
    }

    [Fact]
    public void Desfazer_leitura_so_vale_com_a_contagem_aberta()
    {
        var count = Given();
        count.Scan(Presente, Now);

        Assert.True(count.RemoveScan(Presente));
        Assert.False(count.RemoveScan(Presente));
        Assert.Empty(count.Scans);
    }
}
