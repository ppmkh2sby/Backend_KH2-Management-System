namespace KH2.ManagementSystem.Application.Abstractions.Time;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
