using Inventory.Application.Contracts.Counting;
using Inventory.Application.Contracts.Items;
using Inventory.Application.Contracts.Locations;
using Inventory.Application.Narration;
using Inventory.Application.Services;
using Inventory.Domain.Counting;
using Inventory.Domain.Items;
using Inventory.Domain.Locations;

namespace Inventory.Tests.Fakes;

/// <summary>
/// Monta a aplicação inteira em memória — os mesmos casos de uso que a API usa,
/// com fakes no lugar dos adaptadores. Nenhum banco, nenhum Ollama, nenhum container.
/// </summary>
internal sealed class ApplicationHarness
{
    public ApplicationHarness(bool semanticIndexEnabled = true)
    {
        Index = new FakeNarrativeIndex(semanticIndexEnabled);

        var narrator = new ItemNarrator();
        var committer = new LedgerCommitter(Ledger, Locations, Index, narrator, UnitOfWork);

        LocationService = new LocationService(Locations, Items, UnitOfWork, Clock);
        ItemService = new ItemService(Items, Locations, Ledger, committer, narrator, Clock);
        CountService = new InventoryCountService(Counts, Items, Locations, UnitOfWork, committer, Clock);
        InsightService = new ItemInsightService(Items, Ledger, Locations, Index, narrator, Clock);
    }

    public FakeClock Clock { get; } = new(new DateTimeOffset(2026, 3, 2, 13, 0, 0, TimeSpan.Zero));

    public FakeItemRepository Items { get; } = new();

    public FakeLocationRepository Locations { get; } = new();

    public FakeInventoryCountRepository Counts { get; } = new();

    public FakeItemLedgerRepository Ledger { get; } = new();

    public FakeUnitOfWork UnitOfWork { get; } = new();

    public FakeNarrativeIndex Index { get; }

    public LocationService LocationService { get; }

    public ItemService ItemService { get; }

    public InventoryCountService CountService { get; }

    public ItemInsightService InsightService { get; }

    public async Task<LocationId> GivenLocationAsync(string code, string name)
    {
        var result = await LocationService.CreateAsync(new CreateLocationCommand(code, name, null));

        Assert.True(result.IsSuccess, result.IsSuccess ? "" : result.Error.Message);

        return new LocationId(result.Value.Id);
    }

    public async Task<ItemId> GivenItemAsync(string sku, string name, LocationId location)
    {
        var result = await ItemService.RegisterAsync(
            new RegisterItemCommand(sku, name, null, location.Value, "carga inicial"));

        Assert.True(result.IsSuccess, result.IsSuccess ? "" : result.Error.Message);

        return new ItemId(result.Value.Id);
    }

    public async Task<InventoryCountId> OpenCountAsync(LocationId location, string auditor)
    {
        var result = await CountService.OpenAsync(new OpenInventoryCountCommand(location.Value, auditor));

        Assert.True(result.IsSuccess, result.IsSuccess ? "" : result.Error.Message);

        return new InventoryCountId(result.Value.Id);
    }

    public async Task ScanAsync(InventoryCountId count, string sku)
    {
        var result = await CountService.ScanAsync(count, new ScanItemCommand(sku, null));

        Assert.True(result.IsSuccess, result.IsSuccess ? "" : result.Error.Message);
    }

    public async Task<Item> LoadAsync(ItemId id) =>
        await Items.FindByIdAsync(id) ?? throw new InvalidOperationException($"Item {id} sumiu do fake.");
}
