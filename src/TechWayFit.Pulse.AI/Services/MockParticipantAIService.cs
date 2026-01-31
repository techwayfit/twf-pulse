using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TechWayFit.Pulse.Application.Abstractions.Services;
using TechWayFit.Pulse.Contracts.AI;

namespace TechWayFit.Pulse.AI.Services
{
    public class MockParticipantAIService : IParticipantAIService
    {
        private readonly ILogger<MockParticipantAIService>? _logger;

        public MockParticipantAIService(ILogger<MockParticipantAIService>? logger = null)
        {
            _logger = logger;
        }

        public Task<(ParticipantAnalysisResult? Result, AICallTelemetry? Telemetry)> AnalyzeParticipantResponsesAsync(Guid sessionId, Guid activityId, CancellationToken cancellationToken = default)
        {
            _logger?.LogDebug("Returning mock participant analysis for {Session} {Activity}", sessionId, activityId);
            var result = new ParticipantAnalysisResult
            {
                Summary = "(mock) No AI configured - using fallback analysis",
                Themes = new System.Collections.Generic.List<Theme>
                {
                    new Theme { Name = "Sample Theme", Confidence = 0.8, Evidence = new System.Collections.Generic.List<string> { "keyword1", "keyword2" } }
                },
                SuggestedFollowUp = "Ask participants for specific examples."
            };
            return Task.FromResult<(ParticipantAnalysisResult? Result, AICallTelemetry? Telemetry)>((result, null));
        }
    }
}
