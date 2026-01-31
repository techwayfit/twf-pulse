using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TechWayFit.Pulse.Application.Abstractions.Services;

namespace TechWayFit.Pulse.Web.Controllers
{
    [ApiController]
    [Route("api/ai")]
    public class AiController : ControllerBase
    {
        private readonly IParticipantAIService _participantAI;
        private readonly IFacilitatorAIService _facilitatorAI;
        private readonly ILogger<AiController> _logger;

        public AiController(IParticipantAIService participantAI, IFacilitatorAIService facilitatorAI, ILogger<AiController> logger)
        {
            _participantAI = participantAI;
            _facilitatorAI = facilitatorAI;
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
    }
}
