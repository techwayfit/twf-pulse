using System;
using System.Threading;
using System.Threading.Tasks;
using TechWayFit.Pulse.Contracts.AI;

namespace TechWayFit.Pulse.Application.Abstractions.Services
{
    public interface IParticipantAIService
    {
        /// <summary>
        /// Analyze participant responses for an activity and return an AI-generated summary or insight.
        /// </summary>
        Task<(ParticipantAnalysisResult? Result, AICallTelemetry? Telemetry)> AnalyzeParticipantResponsesAsync(Guid sessionId, Guid activityId, CancellationToken cancellationToken = default);
    }
}
