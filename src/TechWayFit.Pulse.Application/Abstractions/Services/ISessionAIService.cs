using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TechWayFit.Pulse.Contracts.Requests;
using TechWayFit.Pulse.Contracts.Responses;

namespace TechWayFit.Pulse.Application.Abstractions.Services
{
    public interface ISessionAIService
    {
        /// <summary>
        /// Generate a list of agenda activities for a session based on minimal facilitator input.
        /// </summary>
        Task<IReadOnlyList<AgendaActivityResponse>> GenerateSessionActivitiesAsync(CreateSessionRequest request, CancellationToken cancellationToken = default);
    }
}
