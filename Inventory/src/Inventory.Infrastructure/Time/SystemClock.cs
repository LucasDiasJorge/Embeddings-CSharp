using Inventory.Application.Ports.Driven;

namespace Inventory.Infrastructure.Time;

internal sealed class SystemClock : IClock
{
    // UTC sempre. O fuso é problema de quem apresenta, não de quem registra —
    // e um razão de auditoria com horários ambíguos não vale nada.
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
