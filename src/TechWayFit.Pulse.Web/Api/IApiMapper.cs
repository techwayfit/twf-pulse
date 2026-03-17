using TechWayFit.Pulse.Contracts.Models;
using TechWayFit.Pulse.Contracts.Responses;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Domain.Enums;
using TechWayFit.Pulse.Domain.ValueObjects;

namespace TechWayFit.Pulse.Web.Api;

public interface IApiMapper
{
    JoinFormSchema ToDomain(JoinFormSchemaDto dto);

    SessionSettings ToDomain(SessionSettingsDto dto);

    SessionSummaryResponse ToSummary(Session session);

    AgendaActivityResponse ToAgenda(Activity activity);

    ActivityType MapActivityType(ActivityType type);

    SessionStatus MapSessionStatus(SessionStatus status);

    ActivityStatus MapActivityStatus(ActivityStatus status);
}
