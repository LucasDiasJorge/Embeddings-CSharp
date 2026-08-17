using Inventory.Domain.Counting;
using Inventory.Domain.Items;
using Inventory.Domain.Locations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Inventory.Infrastructure.Persistence;

// Os identificadores tipados e os value objects do domínio não são um problema do banco.
// Estes conversores são registrados uma vez em ConfigureConventions e valem para o modelo
// inteiro — inclusive dentro dos tipos owned e nas versões anuláveis.

internal sealed class ItemIdConverter()
    : ValueConverter<ItemId, Guid>(id => id.Value, value => new ItemId(value));

internal sealed class LocationIdConverter()
    : ValueConverter<LocationId, Guid>(id => id.Value, value => new LocationId(value));

internal sealed class InventoryCountIdConverter()
    : ValueConverter<InventoryCountId, Guid>(id => id.Value, value => new InventoryCountId(value));

internal sealed class SkuConverter()
    : ValueConverter<Sku, string>(sku => sku.Value, value => Sku.Create(value));

internal sealed class LocationCodeConverter()
    : ValueConverter<LocationCode, string>(code => code.Value, value => LocationCode.Create(value));
