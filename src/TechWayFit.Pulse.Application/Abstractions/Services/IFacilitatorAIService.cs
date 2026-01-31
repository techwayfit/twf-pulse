using System;
using System.Threading;
using System.Threading.Tasks;
using TechWayFit.Pulse.Contracts.AI;

namespace TechWayFit.Pulse.Application.Abstractions.Services
{
    public interface IFacilitatorAIService
    {
        /// <summary>
        /// Generate facilitator prompts, suggested questions or activities based on session/activity context.
        /// </summary>
        Task<(FacilitatorPromptResult? Result, AICallTelemetry? Telemetry)> GenerateFacilitatorPromptAsync(Guid sessionId, Guid activityId, CancellationToken cancellationToken = default);
    }
}
