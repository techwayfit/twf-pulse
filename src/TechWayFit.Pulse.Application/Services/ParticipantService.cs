using System;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Application.Abstractions.Results;
using TechWayFit.Pulse.Application.Abstractions.Services;
using TechWayFit.Pulse.Application.Commands;
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

    public async Task<Result<Participant>> JoinAsync(
        JoinParticipantCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        try
        {
            var participant = await JoinAsync(
                command.SessionId,
                command.DisplayName,
                command.IsAnonymous,
                command.Dimensions,
                command.JoinedAt,
                cancellationToken);
            return Result<Participant>.Success(participant);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return Result<Participant>.Failure(MapError(ex));
        }
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

            // displayName is passed as a dedicated parameter, not in dimensions — handled separately below
            if (string.Equals(field.Id, "displayName", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!dimensions.TryGetValue(field.Id, out var value) || string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"Join form field '{field.Id}' is required.");
            }
        }

        // Special-case: allow sessions to mark the display name as a required join field
        var displayNameField = schemaFields.FirstOrDefault(f => string.Equals(f.Id, "displayName", StringComparison.OrdinalIgnoreCase));
        if (displayNameField is not null && displayNameField.Required)
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new InvalidOperationException("Display name is required.");
            }
        }

        // Generate authentication token for participant
        var token = Guid.NewGuid().ToString("N");

        var participant = new Participant(
            Guid.NewGuid(),
            sessionId,
            string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim(),
            isAnonymous,
            dimensions,
            joinedAt,
            token);

        await _participants.AddAsync(participant, cancellationToken);
        return participant;
    }

    public Task<IReadOnlyList<Participant>> GetBySessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return _participants.GetBySessionAsync(sessionId, cancellationToken);
    }

    private static Error MapError(Exception ex)
    {
        return ex switch
        {
            ArgumentException argumentException => ResultErrors.Validation(argumentException.Message),
            InvalidOperationException invalidOperationException when invalidOperationException.Message.Contains("not found", StringComparison.OrdinalIgnoreCase)
                => new Error("not_found", invalidOperationException.Message, ErrorType.NotFound),
            InvalidOperationException invalidOperationException => ResultErrors.Validation(invalidOperationException.Message),
            _ => ResultErrors.Unexpected("An unexpected error occurred.")
        };
    }
}
