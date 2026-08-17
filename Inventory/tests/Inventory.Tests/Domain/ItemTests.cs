using Inventory.Domain.Common;
using Inventory.Domain.Counting;
using Inventory.Domain.Items;
using Inventory.Domain.Locations;

namespace Inventory.Tests.Domain;

public class ItemTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 2, 13, 0, 0, TimeSpan.Zero);
    private static readonly LocationId Almoxarifado = LocationId.New();
    private static readonly LocationId Sala204 = LocationId.New();

    private static Item Given() =>
        Item.Register(Sku.Create("pat-001"), "Notebook Dell", null, Almoxarifado, "lucas", Now);

    [Fact]
    public void Register_normaliza_o_sku_e_abre_o_razao()
    {
        var item = Given();

        Assert.Equal("PAT-001", item.Sku.Value);
        Assert.Equal(ItemStatus.Active, item.Status);

        var entry = Assert.Single(item.PendingLedgerEntries);
        Assert.Equal(LedgerEntryKind.Registered, entry.Kind);
        Assert.Equal(1, entry.Sequence);
        Assert.Equal(Almoxarifado, entry.ToLocationId);
    }

    [Fact]
    public void MoveTo_troca_a_localizacao_e_numera_a_linha_do_tempo()
    {
        var item = Given();
        item.ClearPendingLedgerEntries();

        item.MoveTo(Sala204, MovementReason.Transfer, "lucas", "empréstimo", Now.AddDays(1));

        Assert.Equal(Sala204, item.LocationId);
        Assert.Equal(Now.AddDays(1), item.LastSeenAt);

        var entry = Assert.Single(item.PendingLedgerEntries);
        Assert.Equal(2, entry.Sequence);
        Assert.Equal(Almoxarifado, entry.FromLocationId);
        Assert.Equal(Sala204, entry.ToLocationId);
    }

    [Fact]
    public void MoveTo_para_a_mesma_localizacao_e_recusado()
    {
        var item = Given();

        var error = Assert.Throws<DomainException>(
            () => item.MoveTo(Almoxarifado, MovementReason.Transfer, "lucas", null, Now));

        Assert.Equal("item.same_location", error.Code);
    }

    [Fact]
    public void Item_baixado_nao_movimenta_mais()
    {
        var item = Given();
        item.Retire("lucas", "doado", Now);

        var error = Assert.Throws<DomainException>(
            () => item.MoveTo(Sala204, MovementReason.Transfer, "lucas", null, Now));

        Assert.Equal("item.retired", error.Code);
    }

    [Fact]
    public void Movimentar_um_item_faltante_significa_que_ele_foi_encontrado()
    {
        var item = Given();
        item.ReportedMissingByCount(InventoryCountId.New(), "auditor", Now);
        Assert.Equal(ItemStatus.Missing, item.Status);

        item.MoveTo(Sala204, MovementReason.Transfer, "lucas", null, Now.AddDays(2));

        Assert.Equal(ItemStatus.Active, item.Status);
    }

    [Fact]
    public void Dar_como_faltante_nao_mexe_no_LastSeenAt()
    {
        var item = Given();
        var visto = item.LastSeenAt;

        item.ReportedMissingByCount(InventoryCountId.New(), "auditor", Now.AddDays(30));

        // Ninguém viu o item — atualizar LastSeenAt aqui mascararia exatamente
        // a informação que interessa: há quanto tempo ele não é confirmado.
        Assert.Equal(visto, item.LastSeenAt);
        Assert.Equal(Almoxarifado, item.LocationId);
    }

    [Fact]
    public void Confirmacao_de_contagem_renova_a_confianca_sem_mover_nada()
    {
        var item = Given();
        item.ClearPendingLedgerEntries();

        item.ConfirmedByCount(InventoryCountId.New(), "auditor", Now.AddDays(10));

        Assert.Equal(Almoxarifado, item.LocationId);
        Assert.Equal(Now.AddDays(10), item.LastSeenAt);
        Assert.Equal(LedgerEntryKind.CountConfirmed, Assert.Single(item.PendingLedgerEntries).Kind);
    }

    [Fact]
    public void Realocacao_por_contagem_registra_o_motivo_e_a_auditoria_de_origem()
    {
        var item = Given();
        item.ClearPendingLedgerEntries();
        var countId = InventoryCountId.New();

        item.RelocatedByCount(countId, Sala204, "auditor", Now.AddDays(5));

        var entry = Assert.Single(item.PendingLedgerEntries);
        Assert.Equal(LedgerEntryKind.Moved, entry.Kind);
        Assert.Equal(MovementReason.CountReconciliation, entry.Reason);
        Assert.Equal(countId, entry.InventoryCountId);
        Assert.Equal(Sala204, item.LocationId);
    }

    [Fact]
    public void Baixar_duas_vezes_e_recusado()
    {
        var item = Given();
        item.Retire("lucas", null, Now);

        Assert.Equal("item.retired", Assert.Throws<DomainException>(() => item.Retire("lucas", null, Now)).Code);
    }

    [Fact]
    public void Sem_identificar_quem_operou_nao_ha_registro()
    {
        var item = Given();

        Assert.Equal(
            "item.actor_empty",
            Assert.Throws<DomainException>(
                () => item.MoveTo(Sala204, MovementReason.Transfer, "  ", null, Now)).Code);
    }
}
