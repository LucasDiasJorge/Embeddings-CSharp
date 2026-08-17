using Inventory.Application.Common;
using Inventory.Application.Contracts.Counting;
using Inventory.Application.Narration;
using Inventory.Application.Ports.Driven;
using Inventory.Domain.Counting;
using Inventory.Domain.Items;
using Inventory.Domain.Locations;

namespace Inventory.Application.Services;

/// <summary>
/// Orquestra o ciclo de vida da contagem de inventário.
/// </summary>
/// <remarks>
/// O fechamento é o método que justifica a arquitetura inteira: é o único lugar
/// onde a realidade física sobrescreve o sistema em lote, e ele consegue fazer isso
/// apenas conversando com dois agregados — <see cref="InventoryCount"/> dá o veredito,
/// <see cref="Item"/> aplica cada consequência e registra o próprio rastro.
/// Nenhuma linha aqui sabe o que é SQL.
/// </remarks>
public sealed class InventoryCountService(
    IInventoryCountRepository counts,
    IItemRepository items,
    ILocationRepository locations,
    IUnitOfWork unitOfWork,
    LedgerCommitter committer,
    IClock clock)
{
    public async Task<Result<InventoryCountDto>> OpenAsync(
        OpenInventoryCountCommand command, CancellationToken cancellationToken = default)
    {
        var locationId = new LocationId(command.LocationId);
        var location = await locations.FindByIdAsync(locationId, cancellationToken);

        if (location is null)
        {
            return Error.NotFound("location.not_found", $"A localização {locationId} não existe.");
        }

        if (!location.IsActive)
        {
            return Error.Conflict("location.inactive",
                $"A localização {location.Code} está inativa e não pode ser auditada.");
        }

        // Duas contagens abertas no mesmo lugar produziriam vereditos contraditórios
        // sobre os mesmos itens — quem fechasse por último venceria por acidente.
        if (await counts.FindOpenByLocationAsync(locationId, cancellationToken) is { } inProgress)
        {
            return Error.Conflict("count.already_open",
                $"Já existe a contagem {inProgress.Id} aberta em {location.Code} por {inProgress.Auditor}.");
        }

        var expected = await items.ListCountableByLocationAsync(locationId, cancellationToken);

        var count = InventoryCount.Open(
            locationId, command.Auditor, expected.Select(item => item.Id), clock.UtcNow);

        await counts.AddAsync(count, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await DescribeAsync(count, location, cancellationToken);
    }

    public async Task<Result<InventoryCountDto>> ScanAsync(
        InventoryCountId countId, ScanItemCommand command, CancellationToken cancellationToken = default)
    {
        if (await counts.FindByIdAsync(countId, cancellationToken) is not { } count)
        {
            return CountNotFound(countId);
        }

        var resolved = await ResolveAsync(command, cancellationToken);

        if (!resolved.IsSuccess)
        {
            return resolved.Error;
        }

        var item = resolved.Value;

        if (!item.IsCountable)
        {
            return Error.Conflict("item.retired",
                $"O item {item.Sku} está baixado e não deveria estar em circulação. Registre a divergência à parte.");
        }

        count.Scan(item.Id, clock.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await DescribeAsync(count, location: null, cancellationToken);
    }

    public async Task<Result<InventoryCountDto>> RemoveScanAsync(
        InventoryCountId countId, Guid itemId, CancellationToken cancellationToken = default)
    {
        if (await counts.FindByIdAsync(countId, cancellationToken) is not { } count)
        {
            return CountNotFound(countId);
        }

        if (!count.RemoveScan(new ItemId(itemId)))
        {
            return Error.NotFound("count.scan_not_found", $"O item {itemId} não foi lido nesta contagem.");
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await DescribeAsync(count, location: null, cancellationToken);
    }

    public async Task<Result<InventoryCountReportDto>> CloseAsync(
        InventoryCountId countId, CancellationToken cancellationToken = default)
    {
        if (await counts.FindByIdAsync(countId, cancellationToken) is not { } count)
        {
            return CountNotFound(countId);
        }

        var now = clock.UtcNow;
        var reconciliation = count.Close(now);
        var involved = await LoadInvolvedAsync(reconciliation, cancellationToken);

        // Itens baixados entre a abertura e o fechamento saem da conta: eles não sumiram,
        // foram descartados com registro próprio. Cobrá-los distorceria a acuracidade.
        var confirmed = Countable(reconciliation.Confirmed, involved);
        var missing = Countable(reconciliation.Missing, involved);
        var relocated = Countable(reconciliation.Unexpected, involved);

        foreach (var item in confirmed)
        {
            item.ConfirmedByCount(count.Id, count.Auditor, now);
        }

        foreach (var item in missing)
        {
            item.ReportedMissingByCount(count.Id, count.Auditor, now);
        }

        foreach (var item in relocated)
        {
            item.RelocatedByCount(count.Id, count.LocationId, count.Auditor, now);
        }

        await committer.CommitAsync([.. confirmed, .. missing, .. relocated], cancellationToken);

        var location = await locations.FindByIdAsync(count.LocationId, cancellationToken);

        return BuildReport(count, location, confirmed, missing, relocated);
    }

    public async Task<Result<InventoryCountDto>> CancelAsync(
        InventoryCountId countId, CancelInventoryCountCommand command, CancellationToken cancellationToken = default)
    {
        if (await counts.FindByIdAsync(countId, cancellationToken) is not { } count)
        {
            return CountNotFound(countId);
        }

        count.Cancel(command.Reason, clock.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await DescribeAsync(count, location: null, cancellationToken);
    }

    public async Task<Result<InventoryCountDto>> GetAsync(
        InventoryCountId countId, CancellationToken cancellationToken = default) =>
        await counts.FindByIdAsync(countId, cancellationToken) is { } count
            ? await DescribeAsync(count, location: null, cancellationToken)
            : Result<InventoryCountDto>.Failure(CountNotFound(countId));

    public async Task<Result<InventoryCountReportDto>> GetReportAsync(
        InventoryCountId countId, CancellationToken cancellationToken = default)
    {
        if (await counts.FindByIdAsync(countId, cancellationToken) is not { } count)
        {
            return CountNotFound(countId);
        }

        if (count.Status != InventoryCountStatus.Closed)
        {
            return Error.Conflict("count.not_closed",
                $"A contagem {countId} está {count.Status}; o relatório só existe após o fechamento.");
        }

        // Esperados e leituras ficaram gravados, então o veredito é recalculável a qualquer momento.
        var reconciliation = count.Reconcile();
        var involved = await LoadInvolvedAsync(reconciliation, cancellationToken);
        var location = await locations.FindByIdAsync(count.LocationId, cancellationToken);

        return BuildReport(
            count,
            location,
            Countable(reconciliation.Confirmed, involved),
            Countable(reconciliation.Missing, involved),
            Countable(reconciliation.Unexpected, involved));
    }

    public async Task<Result<IReadOnlyList<InventoryCountSummaryDto>>> ListAsync(
        LocationId? locationId, InventoryCountStatus? status, CancellationToken cancellationToken = default)
    {
        var found = await counts.ListAsync(locationId, status, cancellationToken);
        var byId = await locations.FindManyAsync(found.Select(count => count.LocationId).Distinct(), cancellationToken);

        return Result<IReadOnlyList<InventoryCountSummaryDto>>.Success(
        [
            .. found.Select(count => new InventoryCountSummaryDto(
                count.Id.Value,
                count.LocationId.Value,
                byId.GetValueOrDefault(count.LocationId)?.Code.Value ?? string.Empty,
                count.Auditor,
                count.Status.ToString(),
                count.OpenedAt,
                count.ClosedAt,
                count.Expected.Count,
                count.Scans.Count))
        ]);
    }

    private async Task<Result<Item>> ResolveAsync(ScanItemCommand command, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(command.Sku))
        {
            var sku = Sku.Create(command.Sku);

            return await items.FindBySkuAsync(sku, cancellationToken) is { } bySku
                ? Result<Item>.Success(bySku)
                : Result<Item>.Failure(Error.NotFound("item.not_found",
                    $"Nenhum item cadastrado com a etiqueta {sku}."));
        }

        if (command.ItemId is not { } rawId)
        {
            return Error.Validation("count.scan_without_identity",
                "Informe o SKU lido ou o itemId para registrar a leitura.");
        }

        return await items.FindByIdAsync(new ItemId(rawId), cancellationToken) is { } byId
            ? Result<Item>.Success(byId)
            : Result<Item>.Failure(Error.NotFound("item.not_found", $"O item {rawId} não existe."));
    }

    private async Task<IReadOnlyDictionary<ItemId, Item>> LoadInvolvedAsync(
        CountReconciliation reconciliation, CancellationToken cancellationToken)
    {
        var ids = reconciliation.Confirmed
            .Concat(reconciliation.Missing)
            .Concat(reconciliation.Unexpected)
            .Distinct();

        var found = await items.FindManyAsync(ids, cancellationToken);

        return found.ToDictionary(item => item.Id);
    }

    private static IReadOnlyList<Item> Countable(
        IReadOnlyList<ItemId> ids, IReadOnlyDictionary<ItemId, Item> involved) =>
        [.. ids.Select(involved.GetValueOrDefault).OfType<Item>().Where(item => item.IsCountable)];

    private static InventoryCountReportDto BuildReport(
        InventoryCount count,
        Location? location,
        IReadOnlyList<Item> confirmed,
        IReadOnlyList<Item> missing,
        IReadOnlyList<Item> relocated)
    {
        var scannedAt = count.Scans.ToDictionary(scan => scan.ItemId, scan => scan.ScannedAt);
        var expected = confirmed.Count + missing.Count;

        return new InventoryCountReportDto(
            count.Id.Value,
            count.LocationId.Value,
            location?.Code.Value ?? string.Empty,
            count.Auditor,
            count.OpenedAt,
            count.ClosedAt ?? count.OpenedAt,
            expected == 0 ? 1d : (double)confirmed.Count / expected,
            missing.Count == 0 && relocated.Count == 0,
            Describe(confirmed, scannedAt),
            Describe(missing, scannedAt),
            Describe(relocated, scannedAt));
    }

    private async Task<InventoryCountDto> DescribeAsync(
        InventoryCount count, Location? location, CancellationToken cancellationToken)
    {
        location ??= await locations.FindByIdAsync(count.LocationId, cancellationToken);

        var reconciliation = count.Reconcile();
        var involved = await LoadInvolvedAsync(reconciliation, cancellationToken);
        var scannedAt = count.Scans.ToDictionary(scan => scan.ItemId, scan => scan.ScannedAt);

        return new InventoryCountDto(
            count.Id.Value,
            count.LocationId.Value,
            location?.Code.Value ?? string.Empty,
            location?.Name ?? string.Empty,
            count.Auditor,
            count.Status.ToString(),
            count.OpenedAt,
            count.ClosedAt,
            count.CancellationReason,
            count.Expected.Count,
            count.Scans.Count,
            Describe(Resolve(reconciliation.Missing, involved), scannedAt),
            Describe(Resolve(reconciliation.Confirmed, involved), scannedAt),
            Describe(Resolve(reconciliation.Unexpected, involved), scannedAt));
    }

    private static IReadOnlyList<Item> Resolve(
        IReadOnlyList<ItemId> ids, IReadOnlyDictionary<ItemId, Item> involved) =>
        [.. ids.Select(involved.GetValueOrDefault).OfType<Item>()];

    private static IReadOnlyList<CountedItemDto> Describe(
        IReadOnlyList<Item> found, IReadOnlyDictionary<ItemId, DateTimeOffset> scannedAt) =>
        [.. found.Select(item => new CountedItemDto(
            item.Id.Value,
            item.Sku.Value,
            item.Name,
            scannedAt.TryGetValue(item.Id, out var moment) ? moment : null))];

    private static Error CountNotFound(InventoryCountId id) =>
        Error.NotFound("count.not_found", $"A contagem {id} não existe.");
}
