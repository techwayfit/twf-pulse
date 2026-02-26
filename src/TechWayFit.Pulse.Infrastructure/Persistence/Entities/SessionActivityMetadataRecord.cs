namespace TechWayFit.Pulse.Infrastructure.Persistence.Entities;

/// <summary>
/// EF Core persistence record for <see cref="TechWayFit.Pulse.Domain.Entities.SessionActivityMetadata"/>.
/// Maps to the <c>SessionActivityMetadata</c> table.
/// </summary>
public sealed class SessionActivityMetadataRecord
{
    public Guid Id { get; set; }

    public Guid SessionId { get; set; }

    public Guid ActivityId { get; set; }

    /// <summary>Metadata key, max 100 characters. See <c>ActivityMetadataKeys</c>.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Metadata value stored as UTF-8 text (TEXT column).</summary>
    public string Value { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
