using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TechWayFit.Pulse.Application.Abstractions.Services;
using TechWayFit.Pulse.Contracts.AI;

namespace TechWayFit.Pulse.AI.Services
{
    public class MockFiveWhysAIService : IFiveWhysAIService
    {
        private static readonly string[] MockFollowUps =
        {
            "Why did that specific situation arise in the first place?",
            "What caused that underlying condition to exist?",
            "Why was that constraint or gap not identified or addressed earlier?",
            "What process, tool, or ownership gap allowed this to persist?"
        };

        public Task<FiveWhysNextStepResult> GetNextStepAsync(
            string rootQuestion,
            string? context,
            IReadOnlyList<FiveWhysChainEntry> chain,
            int maxDepth = 5,
            CancellationToken cancellationToken = default)
        {
            if (chain.Count >= maxDepth)
            {
                return Task.FromResult(new FiveWhysNextStepResult
                {
                    IsComplete = true,
                    RootCause = "Insufficient process ownership and lack of documented standards in this area.",
                    Insight = "The recurring issue stems from unclear ownership combined with missing process documentation. Recommend assigning a DRI (Directly Responsible Individual) and conducting a 30-day process audit to define clear standards."
                });
            }

            var idx = chain.Count < MockFollowUps.Length ? chain.Count : MockFollowUps.Length - 1;

            return Task.FromResult(new FiveWhysNextStepResult
            {
                NextQuestion = MockFollowUps[idx],
                IsComplete = false
            });
        }
    }
}
