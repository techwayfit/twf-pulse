using TechWayFit.Pulse.Domain.Enums;
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
            activity.ClosedAt,
            activity.DurationMinutes);
    }

    internal static ActivityType MapActivityType(ActivityType type) => type;

    internal static SessionStatus MapSessionStatus(SessionStatus status) => status;

    internal static ActivityStatus MapActivityStatus(ActivityStatus status) => status;

    private static FieldType MapFieldType(FieldType type) => type;
}
