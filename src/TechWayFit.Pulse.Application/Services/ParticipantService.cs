using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Application.Abstractions.Services;
using TechWayFit.Pulse.Domain.Entities;

namespace TechWayFit.Pulse.Application.Services;

public sealed class ParticipantService : IParticipantService
{
    private readonly IParticipantRepository _participants;
    private readonly ISessionRepository _sessions;
    private const int DisplayNameMaxLength = 120;

    public ParticipantService(IParticipantRepository participants, ISessionRepository sessions)
    {
        _participants = participants;
        _sessions = sessions;
    }

    public async Task<Participant> JoinAsync(
        Guid sessionId,
        string? displayName,
        bool isAnonymous,
        IReadOnlyDictionary<string, string?> dimensions,
        DateTimeOffset joinedAt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dimensions);

        if (sessionId == Guid.Empty)
        {
            throw new ArgumentException("Session id is required.", nameof(sessionId));
        }

        if (!string.IsNullOrWhiteSpace(displayName) && displayName.Trim().Length > DisplayNameMaxLength)
        {
            throw new ArgumentException($"Display name must be <= {DisplayNameMaxLength} characters.", nameof(displayName));
        }

        var session = await _sessions.GetByIdAsync(sessionId, cancellationToken);
        if (session is null)
        {
            throw new InvalidOperationException("Session not found.");
        }

        if (!session.Settings.AllowAnonymous && isAnonymous)
        {
            throw new InvalidOperationException("Anonymous participation is disabled for this session.");
        }

        if (dimensions.Count > session.JoinFormSchema.MaxFields)
        {
            throw new InvalidOperationException("Join form exceeds the configured max fields.");
        }

        var schemaFields = session.JoinFormSchema.Fields;
        var validIds = schemaFields.Select(field => field.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var key in dimensions.Keys)
        {
            if (!validIds.Contains(key))
            {
                throw new InvalidOperationException($"Unknown join form field '{key}'.");
            }
        }

        foreach (var field in schemaFields)
        {
            if (!field.Required)
            {
                continue;
            }

            if (!dimensions.TryGetValue(field.Id, out var value) || string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"Join form field '{field.Id}' is required.");
            }
        }

        var participant = new Participant(
            Guid.NewGuid(),
            sessionId,
            string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim(),
            isAnonymous,
            dimensions,
            joinedAt);

        await _participants.AddAsync(participant, cancellationToken);
        return participant;
    }

    public Task<IReadOnlyList<Participant>> GetBySessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return _participants.GetBySessionAsync(sessionId, cancellationToken);
    }
}
