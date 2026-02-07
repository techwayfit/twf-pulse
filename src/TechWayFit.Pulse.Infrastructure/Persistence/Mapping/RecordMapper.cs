using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Domain.Enums;
using TechWayFit.Pulse.Infrastructure.Persistence.Entities;

namespace TechWayFit.Pulse.Infrastructure.Persistence.Mapping;

internal static class RecordMapper
{
    internal static Session ToDomain(this SessionRecord record)
    {
        return new Session(
            record.Id,
            record.Code,
            record.Title,
            record.Goal,
            record.ContextJson,
            PersistenceJson.DeserializeSessionSettings(record.SettingsJson),
            PersistenceJson.DeserializeJoinFormSchema(record.JoinFormSchemaJson),
            (SessionStatus)record.Status,
            record.CurrentActivityId,
            record.CreatedAt,
            record.UpdatedAt,
            record.ExpiresAt,
            record.FacilitatorUserId,
            record.GroupId,
            record.SessionStart,
            record.SessionEnd);
    }

    internal static SessionRecord ToRecord(this Session session)
    {
        return new SessionRecord
        {
            Id = session.Id,
            Code = session.Code,
            Title = session.Title,
            Goal = session.Goal,
            ContextJson = session.Context,
            SettingsJson = PersistenceJson.SerializeSessionSettings(session.Settings),
            JoinFormSchemaJson = PersistenceJson.SerializeJoinFormSchema(session.JoinFormSchema),
            Status = (int)session.Status,
            CurrentActivityId = session.CurrentActivityId,
            CreatedAt = session.CreatedAt,
            UpdatedAt = session.UpdatedAt,
            ExpiresAt = session.ExpiresAt,
            FacilitatorUserId = session.FacilitatorUserId,
            GroupId = session.GroupId,
            SessionStart = session.SessionStart,
            SessionEnd = session.SessionEnd
        };
    }

    internal static Activity ToDomain(this ActivityRecord record)
    {
        return new Activity(
            record.Id,
            record.SessionId,
            record.Order,
            (ActivityType)record.Type,
            record.Title,
            record.Prompt,
            record.ConfigJson,
            (ActivityStatus)record.Status,
            record.OpenedAt,
            record.ClosedAt,
            record.DurationMinutes);
    }

    internal static ActivityRecord ToRecord(this Activity activity)
    {
        return new ActivityRecord
        {
            Id = activity.Id,
            SessionId = activity.SessionId,
            Order = activity.Order,
            Type = (int)activity.Type,
            Title = activity.Title,
            Prompt = activity.Prompt,
            ConfigJson = activity.Config,
            Status = (int)activity.Status,
            OpenedAt = activity.OpenedAt,
            ClosedAt = activity.ClosedAt,
            DurationMinutes = activity.DurationMinutes
        };
    }

    internal static Participant ToDomain(this ParticipantRecord record)
    {
        return new Participant(
            record.Id,
            record.SessionId,
            record.DisplayName,
            record.IsAnonymous,
            PersistenceJson.DeserializeDimensions(record.DimensionsJson),
            record.JoinedAt);
    }

    internal static ParticipantRecord ToRecord(this Participant participant)
    {
        return new ParticipantRecord
        {
            Id = participant.Id,
            SessionId = participant.SessionId,
            DisplayName = participant.DisplayName,
            IsAnonymous = participant.IsAnonymous,
            DimensionsJson = PersistenceJson.SerializeDimensions(participant.Dimensions),
            JoinedAt = participant.JoinedAt
        };
    }

    internal static Response ToDomain(this ResponseRecord record)
    {
        return new Response(
            record.Id,
            record.SessionId,
            record.ActivityId,
            record.ParticipantId,
            record.PayloadJson,
            PersistenceJson.DeserializeDimensions(record.DimensionsJson),
            record.CreatedAt);
    }

    internal static ResponseRecord ToRecord(this Response response)
    {
        return new ResponseRecord
        {
            Id = response.Id,
            SessionId = response.SessionId,
            ActivityId = response.ActivityId,
            ParticipantId = response.ParticipantId,
            PayloadJson = response.Payload,
            DimensionsJson = PersistenceJson.SerializeDimensions(response.Dimensions),
            CreatedAt = response.CreatedAt
        };
    }

    internal static ContributionCounter ToDomain(this ContributionCounterRecord record)
    {
        return new ContributionCounter(
            record.ParticipantId,
            record.SessionId,
            record.TotalContributions,
            record.UpdatedAt);
    }

    internal static ContributionCounterRecord ToRecord(this ContributionCounter counter)
    {
        return new ContributionCounterRecord
        {
            ParticipantId = counter.ParticipantId,
            SessionId = counter.SessionId,
            TotalContributions = counter.TotalContributions,
            UpdatedAt = counter.UpdatedAt
        };
    }
}
