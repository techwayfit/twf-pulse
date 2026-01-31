using System;
using System.Threading;
using System.Threading.Tasks;

namespace TechWayFit.Pulse.Application.Abstractions.Services
{
    public interface IAIWorkQueue
    {
        Task EnqueueAnalysisAsync(Guid sessionId, Guid activityId, CancellationToken cancellationToken = default);
    }
}
