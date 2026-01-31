using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TechWayFit.Pulse.Application.Abstractions.Services;
using TechWayFit.Pulse.Contracts.AI;

namespace TechWayFit.Pulse.AI.Services
{
    public class MockFacilitatorAIService : IFacilitatorAIService
    {
        private readonly ILogger<MockFacilitatorAIService>? _logger;

        public MockFacilitatorAIService(ILogger<MockFacilitatorAIService>? logger = null)
        {
            _logger = logger;
        }

        public Task<(FacilitatorPromptResult? Result, AICallTelemetry? Telemetry)> GenerateFacilitatorPromptAsync(Guid sessionId, Guid activityId, CancellationToken cancellationToken = default)
        {
            _logger?.LogDebug("Returning mock facilitator prompt for {Session} {Activity}", sessionId, activityId);
            var result = new FacilitatorPromptResult
            {
                OpeningStatement = "(mock) No AI configured - Thank you for sharing your thoughts.",
                DiscussionQuestions = new System.Collections.Generic.List<string> { "What happened?", "What was the impact?", "What should we do next?" },
                Tone = "professional",
                SuggestedDuration = "5-7 minutes"
            };
            return Task.FromResult<(FacilitatorPromptResult? Result, AICallTelemetry? Telemetry)>((result, null));
        }
    }
}
