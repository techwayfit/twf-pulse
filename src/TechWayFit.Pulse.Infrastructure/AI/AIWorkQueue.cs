using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TechWayFit.Pulse.Application.Abstractions.Services;

namespace TechWayFit.Pulse.Infrastructure.AI
{
    // Simple in-memory work queue for AI analysis tasks
    public class AIWorkQueue : IAIWorkQueue
    {
        private readonly ConcurrentQueue<(Guid sessionId, Guid activityId)> _queue = new();
        private readonly ILogger<AIWorkQueue> _logger;

        public AIWorkQueue(ILogger<AIWorkQueue> logger)
        {
            _logger = logger;
        }

        public Task EnqueueAnalysisAsync(Guid sessionId, Guid activityId, CancellationToken cancellationToken = default)
        {
            _queue.Enqueue((sessionId, activityId));
            _logger.LogDebug("Enqueued AI analysis for session {Session} activity {Activity}", sessionId, activityId);
            return Task.CompletedTask;
        }

        // Internal helper used by hosted worker
        public bool TryDequeue(out (Guid sessionId, Guid activityId) item) => _queue.TryDequeue(out item);
    }
}
