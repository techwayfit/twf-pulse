using TechWayFit.Pulse.Contracts.Enums;
using TechWayFit.Pulse.Contracts.Models;
using TechWayFit.Pulse.Contracts.Responses;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Domain.ValueObjects;

namespace TechWayFit.Pulse.Web.Api;

internal static class ApiMapper
{
    internal static JoinFormSchema ToDomain(JoinFormSchemaDto dto)
    {
        var fields = dto.Fields.Select(ToDomain).ToList();
        return new JoinFormSchema(dto.MaxFields, fields);
    }

    internal static JoinFormField ToDomain(JoinFormFieldDto dto)
    {
        return new JoinFormField(
            dto.Id,
            dto.Label,
            MapFieldType(dto.Type),
            dto.Required,
            dto.OptionsList, // Use the helper property that returns List<string>
            dto.UseInFilters);
    }

    internal static SessionSettings ToDomain(SessionSettingsDto dto)
    {
        return new SessionSettings(
            dto.MaxContributionsPerParticipantPerSession,
            dto.MaxContributionsPerParticipantPerActivity,
            dto.StrictCurrentActivityOnly,
            dto.AllowAnonymous,
            dto.TtlMinutes);
    }

    internal static SessionSummaryResponse ToSummary(Session session)
    {
        // Convert join form fields from domain to DTO
        var joinFormFields = session.JoinFormSchema.Fields.Select(field => new JoinFormFieldDto
        {
            Id = field.Id,
            Label = field.Label,
            Type = MapFieldType(field.Type),
            Required = field.Required,
            Options = string.Join(",", field.Options), // Convert list to comma-separated string
            UseInFilters = field.UseInFilters
        }).ToList();

        return new SessionSummaryResponse(
            session.Id,
            session.Code,
            session.Title,
            session.Goal,
            MapSessionStatus(session.Status),
            session.CurrentActivityId,
            session.ExpiresAt,
            session.GroupId,
            joinFormFields);
    }

    internal static AgendaActivityResponse ToAgenda(Activity activity)
    {
        return new AgendaActivityResponse(
            activity.Id,
            activity.Order,
            MapActivityType(activity.Type),
            activity.Title,
            activity.Prompt,
            activity.Config,
            MapActivityStatus(activity.Status),
            activity.OpenedAt,
            activity.ClosedAt);
    }

    internal static TechWayFit.Pulse.Domain.Enums.ActivityType MapActivityType(ActivityType type)
    {
        return type switch
        {
            ActivityType.Poll => TechWayFit.Pulse.Domain.Enums.ActivityType.Poll,
            ActivityType.Quiz => TechWayFit.Pulse.Domain.Enums.ActivityType.Quiz,
            ActivityType.WordCloud => TechWayFit.Pulse.Domain.Enums.ActivityType.WordCloud,
            ActivityType.QnA => TechWayFit.Pulse.Domain.Enums.ActivityType.QnA,
            ActivityType.Rating => TechWayFit.Pulse.Domain.Enums.ActivityType.Rating,
            ActivityType.Quadrant => TechWayFit.Pulse.Domain.Enums.ActivityType.Quadrant,
            ActivityType.FiveWhys => TechWayFit.Pulse.Domain.Enums.ActivityType.FiveWhys,
            ActivityType.GeneralFeedback => TechWayFit.Pulse.Domain.Enums.ActivityType.GeneralFeedback,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported activity type.")
        };
    }

    internal static ActivityType MapActivityType(TechWayFit.Pulse.Domain.Enums.ActivityType type)
    {
        return type switch
        {
            TechWayFit.Pulse.Domain.Enums.ActivityType.Poll => ActivityType.Poll,
            TechWayFit.Pulse.Domain.Enums.ActivityType.Quiz => ActivityType.Quiz,
            TechWayFit.Pulse.Domain.Enums.ActivityType.WordCloud => ActivityType.WordCloud,
            TechWayFit.Pulse.Domain.Enums.ActivityType.QnA => ActivityType.QnA,
            TechWayFit.Pulse.Domain.Enums.ActivityType.Rating => ActivityType.Rating,
            TechWayFit.Pulse.Domain.Enums.ActivityType.Quadrant => ActivityType.Quadrant,
            TechWayFit.Pulse.Domain.Enums.ActivityType.FiveWhys => ActivityType.FiveWhys,
            TechWayFit.Pulse.Domain.Enums.ActivityType.GeneralFeedback => ActivityType.GeneralFeedback,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported activity type.")
        };
    }

    internal static SessionStatus MapSessionStatus(TechWayFit.Pulse.Domain.Enums.SessionStatus status)
    {
        return status switch
        {
            TechWayFit.Pulse.Domain.Enums.SessionStatus.Draft => SessionStatus.Draft,
            TechWayFit.Pulse.Domain.Enums.SessionStatus.Live => SessionStatus.Live,
            TechWayFit.Pulse.Domain.Enums.SessionStatus.Ended => SessionStatus.Ended,
            TechWayFit.Pulse.Domain.Enums.SessionStatus.Expired => SessionStatus.Expired,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unsupported session status.")
        };
    }

    internal static TechWayFit.Pulse.Contracts.Enums.ActivityStatus MapActivityStatus(TechWayFit.Pulse.Domain.Enums.ActivityStatus status)
    {
        return status switch
        {
            TechWayFit.Pulse.Domain.Enums.ActivityStatus.Pending => TechWayFit.Pulse.Contracts.Enums.ActivityStatus.Pending,
            TechWayFit.Pulse.Domain.Enums.ActivityStatus.Open => TechWayFit.Pulse.Contracts.Enums.ActivityStatus.Open,
            TechWayFit.Pulse.Domain.Enums.ActivityStatus.Closed => TechWayFit.Pulse.Contracts.Enums.ActivityStatus.Closed,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unsupported activity status.")
        };
    }

    private static TechWayFit.Pulse.Domain.Enums.FieldType MapFieldType(FieldType type)
    {
        return type switch
        {
            FieldType.Text => TechWayFit.Pulse.Domain.Enums.FieldType.Text,
            FieldType.Number => TechWayFit.Pulse.Domain.Enums.FieldType.Number,
            FieldType.Dropdown => TechWayFit.Pulse.Domain.Enums.FieldType.Dropdown,
            FieldType.MultiSelect => TechWayFit.Pulse.Domain.Enums.FieldType.MultiSelect,
            FieldType.Boolean => TechWayFit.Pulse.Domain.Enums.FieldType.Boolean,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported field type.")
        };
    }

    private static FieldType MapFieldType(TechWayFit.Pulse.Domain.Enums.FieldType type)
    {
        return type switch
        {
            TechWayFit.Pulse.Domain.Enums.FieldType.Text => FieldType.Text,
            TechWayFit.Pulse.Domain.Enums.FieldType.Number => FieldType.Number,
            TechWayFit.Pulse.Domain.Enums.FieldType.Dropdown => FieldType.Dropdown,
            TechWayFit.Pulse.Domain.Enums.FieldType.MultiSelect => FieldType.MultiSelect,
            TechWayFit.Pulse.Domain.Enums.FieldType.Boolean => FieldType.Boolean,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported field type.")
        };
    }
}
