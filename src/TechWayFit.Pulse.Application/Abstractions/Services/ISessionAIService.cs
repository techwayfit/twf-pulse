using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TechWayFit.Pulse.Contracts.Requests;
using TechWayFit.Pulse.Contracts.Responses;
using TechWayFit.Pulse.Domain.Entities;

namespace TechWayFit.Pulse.Application.Abstractions.Services
{
    public interface ISessionAIService
    {
        /// <summary>
        /// Generate a list of agenda activities for a session based on minimal facilitator input.
        /// </summary>
        Task<IReadOnlyList<AgendaActivityResponse>> GenerateSessionActivitiesAsync(CreateSessionRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generate and add activities directly to an existing session.
        /// This is the new streamlined approach where session already exists.
        /// </summary>
        Task<IReadOnlyList<AgendaActivityResponse>> GenerateAndAddActivitiesToSessionAsync(
            Session session,
            string? additionalContext,
            string? workshopType,
            int targetActivityCount,
            CancellationToken cancellationToken = default);
    }
}
