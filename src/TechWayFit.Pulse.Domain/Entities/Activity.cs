using TechWayFit.Pulse.Domain.Enums;

namespace TechWayFit.Pulse.Domain.Entities;

public sealed class Activity
{
    public Activity(
        Guid id,
        Guid sessionId,
        int order,
        ActivityType type,
        string title,
        string? prompt,
        string? config,
        ActivityStatus status,
        DateTimeOffset? openedAt,
        DateTimeOffset? closedAt)
    {
        Id = id;
        SessionId = sessionId;
        Order = order;
        Type = type;
        Title = title.Trim();
        Prompt = prompt;
        Config = config;
        Status = status;
        OpenedAt = openedAt;
        ClosedAt = closedAt;
    }

    public Guid Id { get; }

    public Guid SessionId { get; }

    public int Order { get; private set; }

    public ActivityType Type { get; }

    public string Title { get; private set; }

    public string? Prompt { get; private set; }

    public string? Config { get; private set; }

    public ActivityStatus Status { get; private set; }

    public DateTimeOffset? OpenedAt { get; private set; }

    public DateTimeOffset? ClosedAt { get; private set; }

    public void UpdateOrder(int order)
    {
        if (order <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(order), "Order must be greater than zero.");
        }

        Order = order;
    }

    public void Update(string title, string? prompt, string? config)
    {
        Title = title.Trim();
        Prompt = prompt;
        Config = config;
    }

    public void Open(DateTimeOffset openedAt)
    {
        Status = ActivityStatus.Open;
        OpenedAt = openedAt;
        ClosedAt = null;
    }

    public void Close(DateTimeOffset closedAt)
    {
        Status = ActivityStatus.Closed;
        ClosedAt = closedAt;
    }
}
