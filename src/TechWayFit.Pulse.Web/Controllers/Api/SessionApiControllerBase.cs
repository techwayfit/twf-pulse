using Microsoft.AspNetCore.Mvc;
using TechWayFit.Pulse.Application.Abstractions.Results;
using TechWayFit.Pulse.Application.Abstractions.Services;
using TechWayFit.Pulse.Contracts.Models;
using TechWayFit.Pulse.Contracts.Responses;
using TechWayFit.Pulse.Web.Api;

namespace TechWayFit.Pulse.Web.Controllers.Api;

public abstract class SessionApiControllerBase : ControllerBase
{
    private const string FacilitatorTokenHeader = "X-Facilitator-Token";
    private const string ParticipantTokenHeader = "X-Participant-Token";

    private readonly IFacilitatorTokenStore? _facilitatorTokens;
    private readonly IParticipantTokenStore? _participantTokens;

    protected SessionApiControllerBase(
        IFacilitatorTokenStore? facilitatorTokens = null,
        IParticipantTokenStore? participantTokens = null)
    {
        _facilitatorTokens = facilitatorTokens;
        _participantTokens = participantTokens;
    }

    protected static ApiResponse<T> Wrap<T>(T data)
    {
        return new ApiResponse<T>(data);
    }

    protected static ApiResponse<T> Error<T>(string code, string message)
    {
        return new ApiResponse<T>(default, new[] { new ApiError(code, message) });
    }

    protected ActionResult<ApiResponse<T>> FromResult<T>(Result result)
    {
        if (result.IsSuccess)
        {
            throw new InvalidOperationException("Expected failed result but received success.");
        }

        var error = result.Error ?? ResultErrors.Unexpected("Unknown error");
        return FromError<T>(error);
    }

    protected ActionResult<ApiResponse<T>> FromResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
        {
            if (result.Value is null)
            {
                throw new InvalidOperationException("Successful result does not contain a value.");
            }

            return Ok(Wrap(result.Value));
        }

        var error = result.Error ?? ResultErrors.Unexpected("Unknown error");
        return FromError<T>(error);
    }

    private ActionResult<ApiResponse<T>> FromError<T>(Error error)
    {
        var apiError = Error<T>(error.Code, error.Message);
        return error.Type switch
        {
            ErrorType.NotFound => NotFound(apiError),
            ErrorType.Unauthorized => Unauthorized(apiError),
            ErrorType.Forbidden => StatusCode(StatusCodes.Status403Forbidden, apiError),
            ErrorType.RateLimited => StatusCode(StatusCodes.Status429TooManyRequests, apiError),
            ErrorType.Conflict => Conflict(apiError),
            ErrorType.Unexpected => StatusCode(StatusCodes.Status500InternalServerError, apiError),
            _ => BadRequest(apiError)
        };
    }

    protected ActionResult<ApiResponse<T>>? RequireFacilitatorToken<T>(TechWayFit.Pulse.Domain.Entities.Session session)
    {
        var currentContext = Application.Context.FacilitatorContextAccessor.Current;
        if (currentContext != null && currentContext.FacilitatorUserId == session.FacilitatorUserId)
        {
            return null;
        }

        if (_facilitatorTokens is null || !_facilitatorTokens.TryGet(session.Id, out var auth))
        {
            return Unauthorized(Error<T>("facilitator_token_required", "Facilitator token is required."));
        }

        if (Request.Headers.TryGetValue(FacilitatorTokenHeader, out var token)
            && string.Equals(token.ToString(), auth.Token, StringComparison.Ordinal))
        {
            return null;
        }

        return Unauthorized(Error<T>("facilitator_token_required", "Facilitator token is required."));
    }

    protected async Task<ActionResult<ApiResponse<T>>?> RequireParticipantToken<T>(Guid sessionId, Guid participantId)
    {
        if (_participantTokens is null)
        {
            return Unauthorized(Error<T>("participant_token_required", "Participant token validation is unavailable."));
        }

        if (!Request.Headers.TryGetValue(ParticipantTokenHeader, out var token))
        {
            return Unauthorized(Error<T>("participant_token_required", "Participant token is required."));
        }

        if (!await _participantTokens.IsValidAsync(sessionId, participantId, token.ToString()))
        {
            return Unauthorized(Error<T>("invalid_participant_token", "Invalid or mismatched participant token."));
        }

        return null;
    }

    protected static string? BuildEnhancedContext(
        string? additionalContext,
        int? participantCount,
        string? participantType)
    {
        var contextParts = new List<string>();

        if (!string.IsNullOrWhiteSpace(additionalContext))
        {
            contextParts.Add(additionalContext.Trim());
        }

        if (participantCount.HasValue)
        {
            var teamSize = participantCount.Value switch
            {
                <= 5 => "small team",
                <= 10 => "medium-sized team",
                <= 20 => "large team",
                _ => "large group"
            };
            contextParts.Add($"Expected audience: {teamSize} (~{participantCount} participants)");
        }

        if (!string.IsNullOrWhiteSpace(participantType))
        {
            var types = participantType.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var descriptions = types.Select(type => type.ToLowerInvariant() switch
            {
                "developers" => "developers/engineers",
                "product" => "product managers",
                "designers" => "designers/UX",
                "leadership" => "leadership/executives",
                "operations" => "operations/support",
                "sales" => "sales/customer success",
                _ => type
            }).ToArray();

            var audienceDescription = descriptions.Length switch
            {
                1 => $"{descriptions[0]} team",
                2 => $"{descriptions[0]} and {descriptions[1]}",
                _ => $"{string.Join(", ", descriptions.Take(descriptions.Length - 1))}, and {descriptions.Last()}"
            };
            contextParts.Add($"Participant profile: {audienceDescription}");
        }

        return contextParts.Any() ? string.Join(". ", contextParts) : null;
    }

    protected static void ValidateJoinFormSchema(JoinFormSchemaDto schema)
    {
        if (schema?.Fields == null)
        {
            return;
        }

        foreach (var field in schema.Fields)
        {
            if (field.Type == TechWayFit.Pulse.Domain.Enums.FieldType.Dropdown ||
                field.Type == TechWayFit.Pulse.Domain.Enums.FieldType.MultiSelect)
            {
                if (string.IsNullOrWhiteSpace(field.Options))
                {
                    throw new ArgumentException($"Field '{field.Label}' of type '{field.Type}' must have options defined (comma-separated values).");
                }

                var parsedOptions = field.OptionsList;
                if (!parsedOptions.Any())
                {
                    throw new ArgumentException($"Field '{field.Label}' of type '{field.Type}' has invalid options format. Expected comma-separated values, got: '{field.Options}'");
                }
            }
        }
    }
}
