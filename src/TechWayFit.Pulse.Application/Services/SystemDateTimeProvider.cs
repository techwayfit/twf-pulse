using TechWayFit.Pulse.Application.Abstractions.Services;

namespace TechWayFit.Pulse.Application.Services;

public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
