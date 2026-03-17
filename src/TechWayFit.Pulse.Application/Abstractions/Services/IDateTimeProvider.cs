namespace TechWayFit.Pulse.Application.Abstractions.Services;

public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}
