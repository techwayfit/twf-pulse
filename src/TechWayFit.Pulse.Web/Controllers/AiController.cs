using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TechWayFit.Pulse.Application.Abstractions.Services;
using TechWayFit.Pulse.Contracts.AI;

namespace TechWayFit.Pulse.Web.Controllers
{
    [ApiController]
    [Route("api/ai")]
    public class AiController : ControllerBase
    {
        private readonly IParticipantAIService _participantAI;
        private readonly IFacilitatorAIService _facilitatorAI;
        private readonly IFiveWhysAIService _fiveWhysAI;
        private readonly ILogger<AiController> _logger;

        public AiController(
            IParticipantAIService participantAI,
            IFacilitatorAIService facilitatorAI,
            IFiveWhysAIService fiveWhysAI,
            ILogger<AiController> logger)
        {
            _participantAI = participantAI;
            _facilitatorAI = facilitatorAI;
            _fiveWhysAI = fiveWhysAI;
            _logger = logger;
        }

        [HttpGet("participant/analyze")]
        public async Task<IActionResult> AnalyzeParticipant(Guid sessionId, Guid activityId)
        {
            if (sessionId == Guid.Empty || activityId == Guid.Empty)
                return BadRequest(new { error = "sessionId and activityId required" });

            var (result, telemetry) = await _participantAI.AnalyzeParticipantResponsesAsync(sessionId, activityId);
            return Ok(new { result, telemetry });
        }

        [HttpGet("facilitator/prompt")]
        public async Task<IActionResult> GenerateFacilitatorPrompt(Guid sessionId, Guid activityId)
        {
            if (sessionId == Guid.Empty || activityId == Guid.Empty)
                return BadRequest(new { error = "sessionId and activityId required" });

            var (result, telemetry) = await _facilitatorAI.GenerateFacilitatorPromptAsync(sessionId, activityId);
            return Ok(new { result, telemetry });
        }

        /// <summary>
        /// 5 Whys: given the facilitator's root question, context and the current chain of answers,
        /// returns the next follow-up question — or the root cause if the AI has dug deep enough.
        /// </summary>
        [HttpPost("five-whys/next")]
        public async Task<IActionResult> FiveWhysNext([FromBody] FiveWhysNextRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.RootQuestion))
                return BadRequest(new { error = "rootQuestion is required" });

            var result = await _fiveWhysAI.GetNextStepAsync(
                request.RootQuestion,
                request.Context,
                request.Chain ?? new List<FiveWhysChainEntry>(),
                request.MaxDepth);

            return Ok(result);
        }
    }

    /// <summary>Request body for the five-whys/next endpoint.</summary>
    public sealed class FiveWhysNextRequest
    {
        [JsonPropertyName("rootQuestion")]
        public string RootQuestion { get; set; } = string.Empty;

        [JsonPropertyName("context")]
        public string? Context { get; set; }

        [JsonPropertyName("chain")]
        public List<FiveWhysChainEntry>? Chain { get; set; }

        [JsonPropertyName("maxDepth")]
        public int MaxDepth { get; set; } = 5;
    }
}
