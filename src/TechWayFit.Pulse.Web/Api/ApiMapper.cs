using Riok.Mapperly.Abstractions;
using TechWayFit.Pulse.Contracts.Models;
using TechWayFit.Pulse.Contracts.Responses;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Domain.Enums;
using TechWayFit.Pulse.Domain.ValueObjects;

namespace TechWayFit.Pulse.Web.Api;

[Mapper]
public partial class ApiMapper : IApiMapper
{
    public JoinFormSchema ToDomain(JoinFormSchemaDto dto)
    {
        var fields = dto.Fields.Select(ToDomain).ToList();
        return new JoinFormSchema(dto.MaxFields, fields);
    }

    public SessionSummaryResponse ToSummary(Session session)
    {
        var joinFormFields = session.JoinFormSchema.Fields.Select(field => new JoinFormFieldDto
        {
            Id = field.Id,
            Label = field.Label,
            Type = field.Type,
            Required = field.Required,
            Options = string.Join(",", field.Options),
            UseInFilters = field.UseInFilters
        }).ToList();

        return new SessionSummaryResponse(
            session.Id,
            session.Code,
            session.Title,
            session.Goal,
            session.Status,
            session.CurrentActivityId,
            session.ExpiresAt,
            session.GroupId,
            joinFormFields);
    }

    [MapProperty(nameof(Activity.Id), nameof(AgendaActivityResponse.ActivityId))]
    [MapperIgnoreSource(nameof(Activity.SessionId))]
    public partial AgendaActivityResponse ToAgenda(Activity activity);

    public partial SessionSettings ToDomain(SessionSettingsDto dto);

    public ActivityType MapActivityType(ActivityType type) => type;

    public SessionStatus MapSessionStatus(SessionStatus status) => status;

    public ActivityStatus MapActivityStatus(ActivityStatus status) => status;

    private static JoinFormField ToDomain(JoinFormFieldDto dto)
    {
        return new JoinFormField(
            dto.Id,
            dto.Label,
            dto.Type,
            dto.Required,
            dto.OptionsList,
            dto.UseInFilters);
    }
}
