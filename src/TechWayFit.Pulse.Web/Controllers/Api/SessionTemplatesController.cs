using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechWayFit.Pulse.Application.Abstractions.Services;
using TechWayFit.Pulse.Contracts.Models;
using TechWayFit.Pulse.Contracts.Requests;
using TechWayFit.Pulse.Contracts.Responses;
using TechWayFit.Pulse.Domain.Enums;
using TechWayFit.Pulse.Domain.Models;
using TechWayFit.Pulse.Web.Api;
using TechWayFit.Pulse.Web.Extensions;

namespace TechWayFit.Pulse.Web.Controllers.Api;

[ApiController]
[Route("api/templates")]
public class SessionTemplatesController : ControllerBase
{
    private readonly ISessionTemplateService _templateService;
    private readonly IAuthenticationService _authService;
    private readonly ILogger<SessionTemplatesController> _logger;

    public SessionTemplatesController(
        ISessionTemplateService templateService,
        IAuthenticationService authService,
        ILogger<SessionTemplatesController> logger)
    {
        _templateService = templateService;
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Get all available session templates
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<GetTemplatesResponse>> GetTemplates(
        [FromQuery] string? category = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            IReadOnlyList<Domain.Entities.SessionTemplate> templates;

            if (!string.IsNullOrWhiteSpace(category) && Enum.TryParse<TemplateCategory>(category, true, out var templateCategory))
            {
                templates = await _templateService.GetTemplatesByCategoryAsync(templateCategory, cancellationToken);
            }
            else
            {
                templates = await _templateService.GetAllTemplatesAsync(cancellationToken);
            }

            var response = new GetTemplatesResponse
            {
                Templates = templates.Select(MapToDto).ToList()
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get templates");
            return StatusCode(500, new { message = "Failed to retrieve templates" });
        }
    }

    /// <summary>
    /// Get template details with full configuration
    /// </summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<GetTemplateDetailResponse>> GetTemplateDetail(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var template = await _templateService.GetTemplateByIdAsync(id, cancellationToken);
            if (template == null)
            {
                return NotFound(new { message = "Template not found" });
            }

            var config = await _templateService.GetTemplateConfigAsync(id, cancellationToken);
            if (config == null)
            {
                return StatusCode(500, new { message = "Failed to load template configuration" });
            }

            var response = new GetTemplateDetailResponse
            {
                Template = MapToDetailDto(template, config)
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get template {TemplateId}", id);
            return StatusCode(500, new { message = "Failed to retrieve template" });
        }
    }

    /// <summary>
    /// Create a new custom template
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<CreateTemplateResponse>> CreateTemplate(
        [FromBody] CreateSessionTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = await HttpContext.GetFacilitatorUserIdAsync(_authService, cancellationToken);
            if (userId == null)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            if (!Enum.TryParse<TemplateCategory>(request.Category, true, out var category))
            {
                return BadRequest(new { message = "Invalid category" });
            }

            var config = MapToTemplateConfig(request.Config);

            var template = await _templateService.CreateTemplateAsync(
                request.Name,
                request.Description,
                category,
                request.IconEmoji,
                config,
                userId.Value,
                cancellationToken);

            var response = new CreateTemplateResponse
            {
                TemplateId = template.Id,
                Message = "Template created successfully"
            };

            return CreatedAtAction(nameof(GetTemplateDetail), new { id = template.Id }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create template");
            return StatusCode(500, new { message = "Failed to create template" });
        }
    }

    /// <summary>
    /// Update an existing custom template
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<UpdateTemplateResponse>> UpdateTemplate(
        Guid id,
        [FromBody] UpdateSessionTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = await HttpContext.GetFacilitatorUserIdAsync(_authService, cancellationToken);
            if (userId == null)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            if (!Enum.TryParse<TemplateCategory>(request.Category, true, out var category))
            {
                return BadRequest(new { message = "Invalid category" });
            }

            var config = MapToTemplateConfig(request.Config);

            await _templateService.UpdateTemplateAsync(
                id,
                request.Name,
                request.Description,
                category,
                request.IconEmoji,
                config,
                userId.Value,
                cancellationToken);

            var response = new UpdateTemplateResponse
            {
                Message = "Template updated successfully"
            };

            return Ok(response);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update template {TemplateId}", id);
            return StatusCode(500, new { message = "Failed to update template" });
        }
    }

    /// <summary>
    /// Delete a custom template
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteTemplate(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = await HttpContext.GetFacilitatorUserIdAsync(_authService, cancellationToken);
            if (userId == null)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            await _templateService.DeleteTemplateAsync(id, userId.Value, cancellationToken);

            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete template {TemplateId}", id);
            return StatusCode(500, new { message = "Failed to delete template" });
        }
    }

    /// <summary>
    /// Create a session from a template
    /// </summary>
    [HttpPost("create-session")]
    [Authorize]
    public async Task<ActionResult<CreateSessionResponse>> CreateSessionFromTemplate(
        [FromBody] CreateSessionFromTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = await HttpContext.GetFacilitatorUserIdAsync(_authService, cancellationToken);
            if (userId == null)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            SessionTemplateConfig? customizations = null;
            if (request.Customizations != null)
            {
                customizations = new SessionTemplateConfig
                {
                    Title = request.Customizations.Title ?? string.Empty,
                    Goal = request.Customizations.Goal,
                    Context = request.Customizations.Context,
                    Settings = request.Customizations.Settings != null ? new SessionSettingsConfig
                    {
                        MaxParticipants = request.Customizations.Settings.MaxContributionsPerParticipantPerSession
                    } : new SessionSettingsConfig(),
                    JoinFormSchema = request.Customizations.JoinFormSchema != null ? new JoinFormSchemaConfig
                    {
                        Fields = request.Customizations.JoinFormSchema.Fields.Select(f => new JoinFormFieldConfig
                        {
                            Name = f.Id,
                            Label = f.Label,
                            Type = f.Type.ToString(),
                            Required = f.Required,
                            Options = f.OptionsList
                        }).ToList()
                    } : new JoinFormSchemaConfig()
                };
            }

            var session = await _templateService.CreateSessionFromTemplateAsync(
                request.TemplateId,
                userId.Value,
                request.GroupId,
                customizations,
                cancellationToken);

            var response = new CreateSessionResponse(session.Id, session.Code);

            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create session from template {TemplateId}", request.TemplateId);
            return StatusCode(500, new { message = "Failed to create session from template" });
        }
    }

    private static SessionTemplateDto MapToDto(Domain.Entities.SessionTemplate template)
    {
        return new SessionTemplateDto
        {
            Id = template.Id,
            Name = template.Name,
            Description = template.Description,
            Category = template.Category.ToString(),
            IconEmoji = template.IconEmoji,
            IsSystemTemplate = template.IsSystemTemplate,
            CreatedByUserId = template.CreatedByUserId,
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt
        };
    }

    private static SessionTemplateDetailDto MapToDetailDto(Domain.Entities.SessionTemplate template, SessionTemplateConfig config)
    {
        return new SessionTemplateDetailDto
        {
            Id = template.Id,
            Name = template.Name,
            Description = template.Description,
            Category = template.Category.ToString(),
            IconEmoji = template.IconEmoji,
            IsSystemTemplate = template.IsSystemTemplate,
            CreatedByUserId = template.CreatedByUserId,
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt,
            Config = new TechWayFit.Pulse.Contracts.Models.SessionTemplateConfigDto
            {
                Title = config.Title,
                Goal = config.Goal,
                Context = config.Context,
                Settings = new SessionSettingsDto
                {
                    MaxContributionsPerParticipantPerSession = 100,
                    MaxContributionsPerParticipantPerActivity = config.Settings.MaxParticipants,
                    StrictCurrentActivityOnly = true,
                    AllowAnonymous = config.Settings.AllowAnonymous,
                    TtlMinutes = config.Settings.DurationMinutes ?? 120
                },
                JoinFormSchema = new JoinFormSchemaDto
                {
                    MaxFields = 10,
                    Fields = config.JoinFormSchema.Fields.Select(f => new JoinFormFieldDto
                    {
                        Id = f.Name,
                        Label = f.Label,
                        Type = ParseFieldType(f.Type),
                        Required = f.Required,
                        Options = f.Options != null ? string.Join(",", f.Options) : string.Empty,
                        UseInFilters = false
                    }).ToList()
                },
                Activities = config.Activities.Select(a => new ActivityTemplateDto
                {
                    Order = a.Order,
                    Type = a.Type.ToString(),
                    Title = a.Title,
                    Prompt = a.Prompt,
                    Config = a.Config != null ? new ActivityConfigDto
                    {
                        Options = a.Config.Options,
                        MultipleChoice = a.Config.MultipleChoice,
                        CorrectOptionIndex = a.Config.CorrectOptionIndex,
                        MaxRating = a.Config.MaxRating,
                        RatingLabel = a.Config.RatingLabel,
                        XAxisLabel = a.Config.XAxisLabel,
                        YAxisLabel = a.Config.YAxisLabel,
                        TopLeftLabel = a.Config.TopLeftLabel,
                        TopRightLabel = a.Config.TopRightLabel,
                        BottomLeftLabel = a.Config.BottomLeftLabel,
                        BottomRightLabel = a.Config.BottomRightLabel,
                        MaxDepth = a.Config.MaxDepth,
                        MaxWords = a.Config.MaxWords,
                        MinWordLength = a.Config.MinWordLength,
                        Categories = a.Config.Categories
                    } : null
                }).ToList()
            }
        };
    }

    private static TechWayFit.Pulse.Contracts.Enums.FieldType ParseFieldType(string type)
    {
        return type.ToLowerInvariant() switch
        {
            "text" => TechWayFit.Pulse.Contracts.Enums.FieldType.Text,
            "number" => TechWayFit.Pulse.Contracts.Enums.FieldType.Number,
            "select" or "dropdown" => TechWayFit.Pulse.Contracts.Enums.FieldType.Dropdown,
            "multiselect" => TechWayFit.Pulse.Contracts.Enums.FieldType.MultiSelect,
            "boolean" or "checkbox" => TechWayFit.Pulse.Contracts.Enums.FieldType.Boolean,
            _ => TechWayFit.Pulse.Contracts.Enums.FieldType.Text
        };
    }

    private static SessionTemplateConfig MapToTemplateConfig(TechWayFit.Pulse.Contracts.Requests.SessionTemplateConfigDto dto)
    {
        return new SessionTemplateConfig
        {
            Title = dto.Title,
            Goal = dto.Goal,
            Context = dto.Context,
            Settings = new SessionSettingsConfig
            {
                MaxParticipants = dto.Settings.MaxContributionsPerParticipantPerActivity,
                DurationMinutes = dto.Settings.TtlMinutes,
                AllowAnonymous = dto.Settings.AllowAnonymous
            },
            JoinFormSchema = new JoinFormSchemaConfig
            {
                Fields = dto.JoinFormSchema.Fields.Select(f => new JoinFormFieldConfig
                {
                    Name = f.Id,
                    Label = f.Label,
                    Type = f.Type.ToString(),
                    Required = f.Required,
                    Options = f.OptionsList
                }).ToList()
            },
            Activities = dto.Activities.Select(a => new ActivityTemplateConfig
            {
                Order = a.Order,
                Type = Enum.Parse<ActivityType>(a.Type, true),
                Title = a.Title,
                Prompt = a.Prompt,
                Config = a.Config != null ? new ActivityConfigData
                {
                    Options = a.Config.Options,
                    MultipleChoice = a.Config.MultipleChoice,
                    CorrectOptionIndex = a.Config.CorrectOptionIndex,
                    MaxRating = a.Config.MaxRating,
                    RatingLabel = a.Config.RatingLabel,
                    XAxisLabel = a.Config.XAxisLabel,
                    YAxisLabel = a.Config.YAxisLabel,
                    TopLeftLabel = a.Config.TopLeftLabel,
                    TopRightLabel = a.Config.TopRightLabel,
                    BottomLeftLabel = a.Config.BottomLeftLabel,
                    BottomRightLabel = a.Config.BottomRightLabel,
                    MaxDepth = a.Config.MaxDepth,
                    MaxWords = a.Config.MaxWords,
                    MinWordLength = a.Config.MinWordLength,
                    Categories = a.Config.Categories
                } : null
            }).ToList()
        };
    }
}
