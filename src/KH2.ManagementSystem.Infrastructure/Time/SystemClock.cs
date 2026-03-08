using KH2.ManagementSystem.Application.Abstractions.Time;

namespace KH2.ManagementSystem.Infrastructure.Time;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
