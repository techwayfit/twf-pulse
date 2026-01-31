using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TechWayFit.Pulse.Application.Abstractions.Services;
using TechWayFit.Pulse.Contracts.Requests;
using TechWayFit.Pulse.Contracts.Responses;

namespace TechWayFit.Pulse.AI.Services
{
    public class MockSessionAIService : ISessionAIService
    {
        public Task<IReadOnlyList<AgendaActivityResponse>> GenerateSessionActivitiesAsync(CreateSessionRequest request, CancellationToken cancellationToken = default)
        {
            // Return a small sample agenda derived from title/goal for local/dev scenarios
            var list = new List<AgendaActivityResponse>
            {
                new AgendaActivityResponse(Guid.NewGuid(), 1, TechWayFit.Pulse.Contracts.Enums.ActivityType.Poll, "Icebreaker Poll", "What is your single biggest pain right now?", "{ \"options\": [\"Ops\", \"Process\", \"Tooling\"] }", TechWayFit.Pulse.Contracts.Enums.ActivityStatus.Pending, null, null, 3),
                new AgendaActivityResponse(Guid.NewGuid(), 2, TechWayFit.Pulse.Contracts.Enums.ActivityType.WordCloud, "Top Issues Word Cloud", "Describe the top issue in one word.", "{}", TechWayFit.Pulse.Contracts.Enums.ActivityStatus.Pending, null, null, 5),
                new AgendaActivityResponse(Guid.NewGuid(), 3, TechWayFit.Pulse.Contracts.Enums.ActivityType.Quadrant, "Impact vs Effort", "Place items on Impact (Y) and Effort (X).", "{}", TechWayFit.Pulse.Contracts.Enums.ActivityStatus.Pending, null, null, 8),
                new AgendaActivityResponse(Guid.NewGuid(), 4, TechWayFit.Pulse.Contracts.Enums.ActivityType.GeneralFeedback, "Action Ideas", "What one action should we take next?", "{}", TechWayFit.Pulse.Contracts.Enums.ActivityStatus.Pending, null, null, 7)
            };

            return Task.FromResult<IReadOnlyList<AgendaActivityResponse>>(list);
        }
    }
}
