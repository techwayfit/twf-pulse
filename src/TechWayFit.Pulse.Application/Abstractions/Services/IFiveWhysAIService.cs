using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TechWayFit.Pulse.Contracts.AI;

namespace TechWayFit.Pulse.Application.Abstractions.Services
{
    public interface IFiveWhysAIService
    {
        /// <summary>
        /// Given the facilitator's root question, context, and the participant's conversation chain so far,
        /// the AI returns the next follow-up question — or declares the root cause if no further digging is needed.
        /// </summary>
        /// <param name="rootQuestion">The original problem question set by the facilitator.</param>
        /// <param name="context">Optional background context provided by the facilitator.</param>
        /// <param name="chain">All exchanges completed so far (question + answer pairs).</param>
        /// <param name="maxDepth">The maximum allowed depth before forcing a conclusion.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<FiveWhysNextStepResult> GetNextStepAsync(
            string rootQuestion,
            string? context,
            IReadOnlyList<FiveWhysChainEntry> chain,
            int maxDepth = 5,
            CancellationToken cancellationToken = default);
    }
}
