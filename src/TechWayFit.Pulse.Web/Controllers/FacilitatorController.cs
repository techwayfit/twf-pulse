using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Application.Abstractions.Services;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Domain.Models.ActivityConfigs;
using TechWayFit.Pulse.Web.Extensions;
using TechWayFit.Pulse.Web.Models;

namespace TechWayFit.Pulse.Web.Controllers;

[Authorize]
[Route("facilitator")]
public class FacilitatorController : Controller
{
    private readonly ISessionRepository _sessionRepository;
    private readonly ISessionService _sessionService;
    private readonly ISessionGroupService _sessionGroupService;
    private readonly ISessionTemplateService _sessionTemplateService;
    private readonly IActivityService _activityService;
    private readonly IParticipantService _participantService;
    private readonly IResponseService _responseService;
    private readonly IAuthenticationService _authService;
    private readonly ILogger<FacilitatorController> _logger;

    public FacilitatorController(
ISessionRepository sessionRepository,
  ISessionService sessionService,
  ISessionGroupService sessionGroupService,
   ISessionTemplateService sessionTemplateService,
   IActivityService activityService,
   IParticipantService participantService,
   IResponseService responseService,
   IAuthenticationService authService,
   ILogger<FacilitatorController> logger)
    {
        _sessionRepository = sessionRepository;
        _sessionService = sessionService;
        _sessionGroupService = sessionGroupService;
        _sessionTemplateService = sessionTemplateService;
        _activityService = activityService;
        _participantService = participantService;
        _responseService = responseService;
        _authService = authService;
        _logger = logger;
    }

    [HttpGet("report/{code}")]
    public async Task<IActionResult> Report(string code, Guid? participantId = null, CancellationToken cancellationToken = default)
    {
        var userId = await HttpContext.GetFacilitatorUserIdAsync(_authService, cancellationToken);
        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var session = await _sessionRepository.GetByCodeAsync(code, cancellationToken);
        if (session == null)
        {
            return NotFound();
        }

        if (session.FacilitatorUserId != userId)
        {
            return NotFound();
        }

        var participants = await _participantService.GetBySessionAsync(session.Id, cancellationToken);
        var participantLookup = participants.ToDictionary(p => p.Id);
        var agenda = await _activityService.GetAgendaAsync(session.Id, cancellationToken);

        var participantColumns = session.JoinFormSchema.Fields
            .Select(f => new ParticipantFieldColumn
            {
                Id = f.Id,
                Label = string.IsNullOrWhiteSpace(f.Label) ? f.Id : f.Label
            })
            .ToList();

        var participantItems = participants
            .Where(p => !participantId.HasValue || p.Id == participantId.Value)
            .Select(p => new ParticipantReportItem
            {
                ParticipantId = p.Id,
                DisplayName = string.IsNullOrWhiteSpace(p.DisplayName) ? "Anonymous" : p.DisplayName,
                JoinedAt = p.JoinedAt,
                Dimensions = p.Dimensions,
                ResponseCount = 0
            })
            .ToList();

        var participantResponseCounter = participantItems.ToDictionary(x => x.ParticipantId, _ => 0);
        var activityItems = new List<ActivityReportItem>();
        var totalResponses = 0;

        foreach (var activity in agenda.OrderBy(a => a.Order))
        {
            var allResponses = await _responseService.GetByActivityAsync(activity.Id, cancellationToken);
            var filteredResponses = allResponses
                .Where(r => !participantId.HasValue || r.ParticipantId == participantId.Value)
                .OrderBy(r => r.CreatedAt)
                .ToList();
            var isQuadrant = activity.Type == TechWayFit.Pulse.Domain.Enums.ActivityType.Quadrant;
            var quadrantMeta = isQuadrant ? ParseQuadrantReportMetadata(activity.Config) : null;
            var isQnA = activity.Type == TechWayFit.Pulse.Domain.Enums.ActivityType.QnA;
            var isAiSummary = activity.Type == TechWayFit.Pulse.Domain.Enums.ActivityType.AiSummary;
            var aiSummaryReport = isAiSummary ? ParseAiSummaryReportDetails(activity.Config) : null;

            totalResponses += filteredResponses.Count;

            foreach (var response in filteredResponses)
            {
                if (participantResponseCounter.ContainsKey(response.ParticipantId))
                {
                    participantResponseCounter[response.ParticipantId]++;
                }
            }

            var responseRows = isQnA
                ? BuildQnAReportRows(filteredResponses, participantLookup)
                : filteredResponses
                    .Select(r =>
                    {
                        var quadrantDetails = isQuadrant
                            ? ParseQuadrantResponseDetails(r.Payload, quadrantMeta)
                            : null;

                        return new ActivityResponseItem
                        {
                            ResponseId = r.Id,
                            ParticipantId = r.ParticipantId,
                            ParticipantName = participantLookup.TryGetValue(r.ParticipantId, out var p)
                                ? (string.IsNullOrWhiteSpace(p.DisplayName) ? "Anonymous" : p.DisplayName)
                                : "Unknown",
                            CreatedAt = r.CreatedAt,
                            Question = quadrantDetails?.Question,
                            XAxisValue = quadrantDetails?.XValue,
                            YAxisValue = quadrantDetails?.YValue,
                            Summary = BuildResponseSummary(activity.Type, r.Payload)
                        };
                    })
                    .ToList();

            if (isAiSummary && responseRows.Count == 0 && aiSummaryReport is not null)
            {
                responseRows.Add(new ActivityResponseItem
                {
                    ResponseId = activity.Id,
                    ParticipantId = Guid.Empty,
                    ParticipantName = "AI System",
                    CreatedAt = aiSummaryReport.GeneratedAt
                        ?? activity.ClosedAt
                        ?? activity.OpenedAt
                        ?? DateTimeOffset.UtcNow,
                    Summary = aiSummaryReport.Summary
                });
            }

            activityItems.Add(new ActivityReportItem
            {
                ActivityId = activity.Id,
                Order = activity.Order,
                Type = activity.Type.ToString(),
                Title = activity.Title,
                Prompt = activity.Prompt,
                ResponseCount = responseRows.Count,
                OpenedAt = activity.OpenedAt,
                ClosedAt = activity.ClosedAt,
                DurationMinutes = activity.DurationMinutes,
                QuadrantXAxisLabel = isQuadrant ? quadrantMeta?.XAxisLabel : null,
                QuadrantYAxisLabel = isQuadrant ? quadrantMeta?.YAxisLabel : null,
                Chart = new ActivityChartModel(),
                Responses = responseRows
            });
        }

        foreach (var participantItem in participantItems)
        {
            participantItem.ResponseCount = participantResponseCounter.GetValueOrDefault(participantItem.ParticipantId);
        }

        var viewModel = new SessionReportViewModel
        {
            SessionId = session.Id,
            SessionCode = session.Code,
            SessionTitle = session.Title,
            Goal = session.Goal,
            Context = session.Context,
            Status = session.Status.ToString(),
            CreatedAt = session.CreatedAt,
            UpdatedAt = session.UpdatedAt,
            PlannedStart = session.SessionStart,
            PlannedEnd = session.SessionEnd,
            ParticipantCount = participantItems.Count,
            TotalResponses = totalResponses,
            ParticipantColumns = participantColumns,
            Participants = participantItems,
            Activities = activityItems,
            GeneratedAt = DateTimeOffset.UtcNow
        };

        return View(viewModel);
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(int page = 1, int pageSize = 15, CancellationToken cancellationToken = default)
    {
        var userId = await HttpContext.GetFacilitatorUserIdAsync(_authService, cancellationToken);
        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        // Validate pagination parameters
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 20) pageSize = 15;

        var (sessions, totalCount) = await _sessionRepository.GetByFacilitatorUserIdPaginatedAsync(
            userId.Value, 
            page, 
            pageSize, 
            cancellationToken);
        var groups = await _sessionGroupService.GetGroupHierarchyAsync(userId.Value, cancellationToken);

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        ViewData["UserEmail"] = User.FindFirst(ClaimTypes.Email)?.Value;
        ViewData["UserName"] = User.FindFirst(ClaimTypes.Name)?.Value;
        ViewData["Groups"] = groups;
        ViewData["CurrentPage"] = page;
        ViewData["PageSize"] = pageSize;
        ViewData["TotalCount"] = totalCount;
        ViewData["TotalPages"] = totalPages;

        return View(sessions);
    }


    /// <summary>
    /// Create session page - static form with JavaScript enhancement
    /// </summary>
    [HttpGet("create-session")]
    [HttpGet("create")] // Backward compatibility
    public async Task<IActionResult> CreateSession(Guid? groupId = null, Guid? templateId = null, CancellationToken cancellationToken = default)
    {
        var userId = await HttpContext.GetFacilitatorUserIdAsync(_authService, cancellationToken);
        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var groups = await _sessionGroupService.GetFacilitatorGroupsAsync(userId.Value, cancellationToken);
        ViewData["Groups"] = groups;
        ViewData["SelectedGroupId"] = groupId;
        ViewData["TemplateId"] = templateId;

        // Load template data if templateId is provided
        if (templateId.HasValue)
        {
            _logger.LogInformation("Loading template {TemplateId}", templateId.Value);
            var template = await _sessionTemplateService.GetTemplateByIdAsync(templateId.Value, cancellationToken);
            if (template != null)
            {
                _logger.LogInformation("Template found: {Name}", template.Name);
                var config = await _sessionTemplateService.GetTemplateConfigAsync(templateId.Value, cancellationToken);
                if (config != null)
                {
                    ViewData["TemplateName"] = config.Title;
                    ViewData["TemplateDescription"] = config.Goal;
                    ViewData["TemplateContext"] = config.Context;
                    ViewData["TemplateConfigJson"] = System.Text.Json.JsonSerializer.Serialize(config, new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                    });
                }
                else
                {
                    ViewData["TemplateName"] = template.Name;
                    ViewData["TemplateDescription"] = template.Description;
                }
            }
            else
            {
                _logger.LogWarning("Template {TemplateId} not found", templateId.Value);
            }
        }

        return View();
    }

    /// <summary>
    /// Add Activities page - choose between manual, template, or AI activity creation
    /// </summary>
    [HttpGet("add-activities")]
    public async Task<IActionResult> AddActivities(string code, Guid? templateId = null, CancellationToken cancellationToken = default)
    {
        var userId = await HttpContext.GetFacilitatorUserIdAsync(_authService, cancellationToken);
        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        // Validate session code from query string
        if (string.IsNullOrWhiteSpace(code))
        {
            _logger.LogWarning("AddActivities accessed without session code");
            return NotFound();
        }

        // Verify session exists and belongs to user
        var session = await _sessionRepository.GetByCodeAsync(code, cancellationToken);
        if (session == null)
        {
            _logger.LogWarning("Session not found: {Code}", code);
            return NotFound();
        }

        if (session.FacilitatorUserId != userId)
        {
            _logger.LogWarning("Unauthorized access attempt to session {Code} by user {UserId}", code, userId);
            return NotFound(); // Don't reveal if session exists
        }

        // Pass session data to view
        ViewData["SessionCode"] = session.Code;
        ViewData["SessionTitle"] = session.Title;
        ViewData["SessionId"] = session.Id;
        ViewData["TemplateId"] = templateId;

        return View();
    }

    /// <summary>
    /// Session Groups management page
    /// </summary>
    [HttpGet("groups")]
    public async Task<IActionResult> Groups(CancellationToken cancellationToken = default)
    {
        var userId = await HttpContext.GetFacilitatorUserIdAsync(_authService, cancellationToken);
        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var groups = await _sessionGroupService.GetGroupHierarchyAsync(userId.Value, cancellationToken);
        var allSessions = await _sessionRepository.GetByFacilitatorUserIdAsync(userId.Value, cancellationToken);
        
        // Build view models with session data
        var groupViewModels = groups.Select(g => new Web.Models.GroupWithSessionsViewModel
        {
            Group = g,
            Sessions = allSessions
                .Where(s => s.GroupId == g.Id)
                .OrderBy(s => s.Title)
                .Select(s => new Web.Models.SessionSummary
                {
                    Id = s.Id,
                    Title = s.Title.Length > 50 ? s.Title.Substring(0, 47) + "..." : s.Title,
                    SessionCode = s.Code,
                    Status = s.Status.ToString(),
                    ExpiresAt = s.ExpiresAt,
                    IsCompleted = s.Status == Domain.Enums.SessionStatus.Ended || 
                                 s.Status == Domain.Enums.SessionStatus.Expired,
                    IsActive = s.Status == Domain.Enums.SessionStatus.Live && s.ExpiresAt > DateTimeOffset.UtcNow,
                    SessionStart = s.SessionStart,
                    SessionEnd = s.SessionEnd
                })
                .ToList(),
            TotalSessionCount = GetTotalSessionCount(g.Id, groups, allSessions)
        }).ToList();
        
        ViewData["UserEmail"] = User.FindFirst(ClaimTypes.Email)?.Value;
        ViewData["UserName"] = User.FindFirst(ClaimTypes.Name)?.Value;
        
        return View(groupViewModels);
    }

    private int GetTotalSessionCount(Guid groupId, IReadOnlyCollection<SessionGroup> allGroups, IReadOnlyList<Session> allSessions)
    {
        // Count direct sessions
        var count = allSessions.Count(s => s.GroupId == groupId);
        
        // Count sessions in child groups recursively
        var childGroups = allGroups.Where(g => g.ParentGroupId == groupId);
        foreach (var child in childGroups)
        {
            count += GetTotalSessionCount(child.Id, allGroups, allSessions);
        }
        
        return count;
    }

    /// <summary>
    /// Template browser page - list all available templates
    /// </summary>
    [HttpGet("templates")]
    public async Task<IActionResult> Templates(CancellationToken cancellationToken = default)
    {
        var userId = await HttpContext.GetFacilitatorUserIdAsync(_authService, cancellationToken);
        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var templates = await _sessionTemplateService.GetAllTemplatesAsync(cancellationToken);
        
        ViewData["UserEmail"] = User.FindFirst(ClaimTypes.Email)?.Value;
        ViewData["UserName"] = User.FindFirst(ClaimTypes.Name)?.Value;
        
        return View(templates);
    }

    /// <summary>
    /// Edit Session page
    /// </summary>
    [HttpGet("edit-session/{code}")]
    public async Task<IActionResult> EditSession(string code, string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        var userId = await HttpContext.GetFacilitatorUserIdAsync(_authService, cancellationToken);
        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        // Get session
        var session = await _sessionRepository.GetByCodeAsync(code, cancellationToken);
        if (session == null)
        {
            _logger.LogWarning("Session not found: {Code}", code);
            return NotFound();
        }

        // Verify ownership
        if (session.FacilitatorUserId != userId)
        {
            _logger.LogWarning("Unauthorized access attempt to session {Code} by user {UserId}", code, userId);
            return NotFound();
        }

        // Get groups for dropdown
        var groups = await _sessionGroupService.GetFacilitatorGroupsAsync(userId.Value, cancellationToken);
        
        ViewData["Groups"] = groups;
        ViewData["ReturnUrl"] = returnUrl ?? Url.Action("Dashboard", "Facilitator");
        
        return View(session);
    }

    /// <summary>
    /// Serve activity form modals partial view for dynamic loading
    /// </summary>
    [HttpGet("activity-modals")]
    [AllowAnonymous] // Allow Blazor pages to fetch this
    public IActionResult ActivityModals()
    {
        return PartialView("~/Views/Shared/_ActivityFormModals.cshtml");
    }

    private static string BuildResponseSummary(TechWayFit.Pulse.Domain.Enums.ActivityType type, string payload)
    {
        try
        {
            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;

            return type switch
            {
                TechWayFit.Pulse.Domain.Enums.ActivityType.Poll => BuildPollSummary(root),
                TechWayFit.Pulse.Domain.Enums.ActivityType.WordCloud => root.TryGetProperty("text", out var text)
                    ? text.GetString() ?? ""
                    : payload,
                TechWayFit.Pulse.Domain.Enums.ActivityType.Rating => BuildRatingSummary(root),
                TechWayFit.Pulse.Domain.Enums.ActivityType.GeneralFeedback => BuildFeedbackSummary(root),
                TechWayFit.Pulse.Domain.Enums.ActivityType.Quadrant => BuildQuadrantSummary(root),
                TechWayFit.Pulse.Domain.Enums.ActivityType.QnA => BuildQnASummary(root),
                _ => payload
            };
        }
        catch
        {
            return payload;
        }
    }

    private static ActivityChartModel BuildChart(
        TechWayFit.Pulse.Domain.Enums.ActivityType type,
        string? config,
        IReadOnlyList<Response> responses)
    {
        return type switch
        {
            TechWayFit.Pulse.Domain.Enums.ActivityType.Poll => BuildPollChart(config, responses),
            TechWayFit.Pulse.Domain.Enums.ActivityType.WordCloud => BuildWordCloudChart(responses),
            TechWayFit.Pulse.Domain.Enums.ActivityType.Rating => BuildRatingChart(responses),
            TechWayFit.Pulse.Domain.Enums.ActivityType.GeneralFeedback => BuildFeedbackChart(responses),
            TechWayFit.Pulse.Domain.Enums.ActivityType.Quadrant => BuildQuadrantChart(responses),
            _ => new ActivityChartModel { Type = "bar", Title = "Responses", Labels = ["Responses"], Values = [responses.Count] }
        };
    }

    private static ActivityChartModel BuildPollChart(string? config, IReadOnlyList<Response> responses)
    {
        var optionLabels = ParsePollOptionLabels(config);
        var counts = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        foreach (var response in responses)
        {
            try
            {
                using var doc = JsonDocument.Parse(response.Payload);
                var root = doc.RootElement;

                if (root.TryGetProperty("selectedOptionIds", out var selected) && selected.ValueKind == JsonValueKind.Array)
                {
                    foreach (var option in selected.EnumerateArray())
                    {
                        var optionId = option.GetString();
                        if (string.IsNullOrWhiteSpace(optionId))
                        {
                            continue;
                        }

                        var label = optionLabels.TryGetValue(optionId, out var mapped) ? mapped : optionId;
                        counts[label] = counts.GetValueOrDefault(label) + 1;
                    }
                }
            }
            catch
            {
                // Ignore malformed response payloads for charting
            }
        }

        var ordered = counts.OrderByDescending(x => x.Value).ToList();
        return new ActivityChartModel
        {
            Type = "bar",
            Title = "Poll Selections",
            Labels = ordered.Select(x => x.Key).ToList(),
            Values = ordered.Select(x => x.Value).ToList()
        };
    }

    private static ActivityChartModel BuildWordCloudChart(IReadOnlyList<Response> responses)
    {
        var counts = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        foreach (var response in responses)
        {
            try
            {
                using var doc = JsonDocument.Parse(response.Payload);
                if (!doc.RootElement.TryGetProperty("text", out var textElement))
                {
                    continue;
                }

                var text = textElement.GetString();
                if (string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                var words = Regex.Split(text, @"\s+")
                    .Select(w => Regex.Replace(w, @"[^\p{L}\p{Nd}'-]", ""))
                    .Where(w => !string.IsNullOrWhiteSpace(w));

                foreach (var word in words)
                {
                    counts[word] = counts.GetValueOrDefault(word) + 1;
                }
            }
            catch
            {
                // Ignore malformed response payloads for charting
            }
        }

        var top = counts
            .OrderByDescending(x => x.Value)
            .ThenBy(x => x.Key)
            .Take(15)
            .ToList();

        return new ActivityChartModel
        {
            Type = "bar",
            Title = "Top Words",
            Labels = top.Select(x => x.Key).ToList(),
            Values = top.Select(x => x.Value).ToList()
        };
    }

    private static ActivityChartModel BuildRatingChart(IReadOnlyList<Response> responses)
    {
        var counts = new Dictionary<int, double>();

        foreach (var response in responses)
        {
            try
            {
                using var doc = JsonDocument.Parse(response.Payload);
                if (doc.RootElement.TryGetProperty("rating", out var ratingElement)
                    && ratingElement.ValueKind == JsonValueKind.Number
                    && ratingElement.TryGetInt32(out var rating))
                {
                    counts[rating] = counts.GetValueOrDefault(rating) + 1;
                }
            }
            catch
            {
                // Ignore malformed response payloads for charting
            }
        }

        var ordered = counts.OrderBy(x => x.Key).ToList();
        return new ActivityChartModel
        {
            Type = "bar",
            Title = "Rating Distribution",
            Labels = ordered.Select(x => x.Key.ToString()).ToList(),
            Values = ordered.Select(x => x.Value).ToList()
        };
    }

    private static ActivityChartModel BuildFeedbackChart(IReadOnlyList<Response> responses)
    {
        var counts = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        foreach (var response in responses)
        {
            try
            {
                using var doc = JsonDocument.Parse(response.Payload);
                var category = "Uncategorized";
                if (doc.RootElement.TryGetProperty("category", out var categoryElement)
                    && categoryElement.ValueKind == JsonValueKind.String
                    && !string.IsNullOrWhiteSpace(categoryElement.GetString()))
                {
                    category = categoryElement.GetString()!;
                }

                counts[category] = counts.GetValueOrDefault(category) + 1;
            }
            catch
            {
                // Ignore malformed response payloads for charting
            }
        }

        var ordered = counts.OrderByDescending(x => x.Value).ToList();
        return new ActivityChartModel
        {
            Type = "bar",
            Title = "Feedback by Category",
            Labels = ordered.Select(x => x.Key).ToList(),
            Values = ordered.Select(x => x.Value).ToList()
        };
    }

    private static ActivityChartModel BuildQuadrantChart(IReadOnlyList<Response> responses)
    {
        var points = new List<ActivityScatterPoint>();

        foreach (var response in responses)
        {
            try
            {
                using var doc = JsonDocument.Parse(response.Payload);
                var root = doc.RootElement;

                if (!TryGetNumericValue(root, "xValue", "x", out var x)
                    || !TryGetNumericValue(root, "yValue", "y", out var y))
                {
                    continue;
                }

                var label = TryGetStringValue(root, "label");
                if (string.IsNullOrWhiteSpace(label)
                    && TryGetPropertyCaseInsensitive(root, "itemIndex", out var itemIndexElement)
                    && itemIndexElement.TryGetInt32(out var itemIndex))
                {
                    label = $"Item {itemIndex + 1}";
                }

                points.Add(new ActivityScatterPoint
                {
                    X = x,
                    Y = y,
                    Label = label ?? string.Empty
                });
            }
            catch
            {
                // Ignore malformed response payloads for charting
            }
        }

        return new ActivityChartModel
        {
            Type = "scatter",
            Title = "Quadrant Distribution",
            Points = points
        };
    }

    private static Dictionary<string, string> ParsePollOptionLabels(string? config)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(config))
        {
            return result;
        }

        try
        {
            using var doc = JsonDocument.Parse(config);
            if (!doc.RootElement.TryGetProperty("options", out var options) || options.ValueKind != JsonValueKind.Array)
            {
                return result;
            }

            var index = 0;
            foreach (var option in options.EnumerateArray())
            {
                if (option.ValueKind == JsonValueKind.String)
                {
                    var label = option.GetString();
                    if (!string.IsNullOrWhiteSpace(label))
                    {
                        result[$"option_{index}"] = label;
                    }
                }
                else if (option.ValueKind == JsonValueKind.Object)
                {
                    var id = option.TryGetProperty("id", out var idElement)
                        ? idElement.GetString()
                        : null;
                    var label = option.TryGetProperty("label", out var labelElement)
                        ? labelElement.GetString()
                        : null;

                    if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(label))
                    {
                        result[id] = label;
                    }
                }

                index++;
            }
        }
        catch
        {
            // Ignore malformed config
        }

        return result;
    }

    private static string BuildPollSummary(JsonElement root)
    {
        var options = new List<string>();
        if (root.TryGetProperty("selectedOptionIds", out var selected) && selected.ValueKind == JsonValueKind.Array)
        {
            options.AddRange(selected.EnumerateArray().Select(x => x.GetString()).Where(x => !string.IsNullOrWhiteSpace(x))!);
        }

        var custom = root.TryGetProperty("customOptionText", out var customElement)
            ? customElement.GetString()
            : null;

        if (!string.IsNullOrWhiteSpace(custom))
        {
            options.Add($"custom: {custom}");
        }

        return options.Count > 0 ? string.Join(", ", options) : "No selection";
    }

    private static string BuildRatingSummary(JsonElement root)
    {
        var rating = root.TryGetProperty("rating", out var ratingElement) && ratingElement.TryGetInt32(out var value)
            ? value
            : 0;
        var comment = root.TryGetProperty("comment", out var commentElement)
            ? commentElement.GetString()
            : null;

        return string.IsNullOrWhiteSpace(comment)
            ? $"Rating: {rating}"
            : $"Rating: {rating}; Comment: {comment}";
    }

    private static string BuildFeedbackSummary(JsonElement root)
    {
        var content = root.TryGetProperty("content", out var contentElement)
            ? contentElement.GetString()
            : null;
        var category = root.TryGetProperty("category", out var categoryElement)
            ? categoryElement.GetString()
            : null;

        if (string.IsNullOrWhiteSpace(content))
        {
            return "(empty feedback)";
        }

        return string.IsNullOrWhiteSpace(category)
            ? content
            : $"[{category}] {content}";
    }

    private static string BuildQuadrantSummary(JsonElement root)
    {
        var x = GetDisplayValue(root, "xValue", "x");
        var y = GetDisplayValue(root, "yValue", "y");

        var label = TryGetStringValue(root, "label");
        if (string.IsNullOrWhiteSpace(label)
            && TryGetPropertyCaseInsensitive(root, "itemIndex", out var idxElement)
            && idxElement.TryGetInt32(out var idx))
        {
            label = $"Item {idx + 1}";
        }

        return string.IsNullOrWhiteSpace(label)
            ? $"x={x}, y={y}"
            : $"{label} (x={x}, y={y})";
    }

    private static string BuildQnASummary(JsonElement root)
    {
        var type = TryGetStringValue(root, "type");
        var text = GetQnAText(root);
        var questionResponseId = GetQnAQuestionResponseId(root);

        if (string.Equals(type, "question", StringComparison.OrdinalIgnoreCase)
            || (string.IsNullOrWhiteSpace(type) && !string.IsNullOrWhiteSpace(text)))
        {
            return string.IsNullOrWhiteSpace(text) ? "(empty question)" : text;
        }

        if (string.Equals(type, "vote", StringComparison.OrdinalIgnoreCase)
            || (string.IsNullOrWhiteSpace(type) && !string.IsNullOrWhiteSpace(questionResponseId)))
        {
            return "Upvote";
        }

        return root.ToString();
    }

    private static List<ActivityResponseItem> BuildQnAReportRows(
        IReadOnlyList<Response> responses,
        IReadOnlyDictionary<Guid, Participant> participantLookup)
    {
        var questionRows = new List<ActivityResponseItem>();
        var questionByResponseId = new Dictionary<string, ActivityResponseItem>(StringComparer.OrdinalIgnoreCase);
        var pendingVotes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var response in responses.OrderBy(r => r.CreatedAt))
        {
            if (string.IsNullOrWhiteSpace(response.Payload))
            {
                continue;
            }

            try
            {
                using var doc = JsonDocument.Parse(NormalizeJsonPayload(response.Payload));
                var root = doc.RootElement;
                if (root.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var type = TryGetStringValue(root, "type");
                var text = GetQnAText(root);
                var questionResponseId = GetQnAQuestionResponseId(root);
                var isQuestionPayload = string.Equals(type, "question", StringComparison.OrdinalIgnoreCase)
                    || (string.IsNullOrWhiteSpace(type) && !string.IsNullOrWhiteSpace(text));
                var isVotePayload = string.Equals(type, "vote", StringComparison.OrdinalIgnoreCase)
                    || (string.IsNullOrWhiteSpace(type) && !string.IsNullOrWhiteSpace(questionResponseId));

                if (isQuestionPayload)
                {
                    if (string.IsNullOrWhiteSpace(text))
                    {
                        continue;
                    }

                    var isAnonymous = false;
                    if (TryGetPropertyCaseInsensitive(root, "isAnonymous", out var anonymousElement))
                    {
                        if (anonymousElement.ValueKind is JsonValueKind.True or JsonValueKind.False)
                        {
                            isAnonymous = anonymousElement.GetBoolean();
                        }
                        else if (anonymousElement.ValueKind == JsonValueKind.String)
                        {
                            bool.TryParse(anonymousElement.GetString(), out isAnonymous);
                        }
                    }

                    var participantName = participantLookup.TryGetValue(response.ParticipantId, out var participant)
                        ? (string.IsNullOrWhiteSpace(participant.DisplayName) ? "Anonymous" : participant.DisplayName)
                        : "Unknown";

                    if (isAnonymous)
                    {
                        participantName = "Anonymous";
                    }

                    var responseId = response.Id.ToString();
                    pendingVotes.TryGetValue(responseId, out var votes);
                    pendingVotes.Remove(responseId);

                    var row = new ActivityResponseItem
                    {
                        ResponseId = response.Id,
                        ParticipantId = response.ParticipantId,
                        ParticipantName = participantName,
                        CreatedAt = response.CreatedAt,
                        Summary = text.Trim(),
                        UpvoteCount = votes
                    };

                    questionRows.Add(row);
                    questionByResponseId[responseId] = row;
                }
                else if (isVotePayload)
                {
                    if (string.IsNullOrWhiteSpace(questionResponseId))
                    {
                        continue;
                    }

                    if (questionByResponseId.TryGetValue(questionResponseId, out var questionRow))
                    {
                        questionRow.UpvoteCount = (questionRow.UpvoteCount ?? 0) + 1;
                    }
                    else
                    {
                        pendingVotes[questionResponseId] = pendingVotes.GetValueOrDefault(questionResponseId) + 1;
                    }
                }
            }
            catch
            {
                // Ignore malformed payloads.
            }
        }

        return questionRows;
    }

    private static string? GetQnAText(JsonElement root)
    {
        var text = TryGetStringValue(root, "text");
        if (!string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        text = TryGetStringValue(root, "content");
        if (!string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        return TryGetStringValue(root, "question");
    }

    private static string? GetQnAQuestionResponseId(JsonElement root)
    {
        var id = TryGetStringValue(root, "questionResponseId");
        if (!string.IsNullOrWhiteSpace(id))
        {
            return id;
        }

        id = TryGetStringValue(root, "questionId");
        if (!string.IsNullOrWhiteSpace(id))
        {
            return id;
        }

        return TryGetStringValue(root, "targetResponseId");
    }

    private static string NormalizeJsonPayload(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return payload;
        }

        var trimmed = payload.Trim();
        if (!trimmed.StartsWith("\"", StringComparison.Ordinal))
        {
            return payload;
        }

        try
        {
            var unwrapped = JsonSerializer.Deserialize<string>(trimmed);
            return string.IsNullOrWhiteSpace(unwrapped) ? payload : unwrapped;
        }
        catch
        {
            return payload;
        }
    }

    private static AiSummaryReportDetails? ParseAiSummaryReportDetails(string? config)
    {
        if (string.IsNullOrWhiteSpace(config))
        {
            return null;
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<AiSummaryConfig>(
                config,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (parsed is null)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(parsed.GeneratedSummary))
            {
                return new AiSummaryReportDetails
                {
                    Summary = parsed.GeneratedSummary.Trim(),
                    GeneratedAt = parsed.GeneratedAt
                };
            }

            if (parsed.IsGenerating)
            {
                return new AiSummaryReportDetails
                {
                    Summary = "AI summary generation is in progress.",
                    GeneratedAt = parsed.GeneratedAt
                };
            }
        }
        catch
        {
            // Ignore malformed config and return no report details.
        }

        return null;
    }

    private static QuadrantReportMetadata ParseQuadrantReportMetadata(string? config)
    {
        var metadata = new QuadrantReportMetadata
        {
            XAxisLabel = "Complexity",
            YAxisLabel = "Effort",
            Items = []
        };

        if (string.IsNullOrWhiteSpace(config))
        {
            return metadata;
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<QuadrantConfig>(
                config,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (parsed is null)
            {
                return metadata;
            }

            metadata.XAxisLabel = string.IsNullOrWhiteSpace(parsed.XAxisLabel) ? metadata.XAxisLabel : parsed.XAxisLabel;
            metadata.YAxisLabel = string.IsNullOrWhiteSpace(parsed.YAxisLabel) ? metadata.YAxisLabel : parsed.YAxisLabel;
            metadata.Items = parsed.Items ?? [];
        }
        catch
        {
            // Ignore malformed config and fall back to defaults
        }

        return metadata;
    }

    private static QuadrantResponseDetails ParseQuadrantResponseDetails(string payload, QuadrantReportMetadata? metadata)
    {
        var result = new QuadrantResponseDetails
        {
            Question = "-",
            XValue = "-",
            YValue = "-"
        };

        if (string.IsNullOrWhiteSpace(payload))
        {
            return result;
        }

        try
        {
            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;

            result.XValue = GetDisplayValue(root, "xValue", "x");
            result.YValue = GetDisplayValue(root, "yValue", "y");

            if (TryGetPropertyCaseInsensitive(root, "itemIndex", out var itemIndexElement)
                && itemIndexElement.TryGetInt32(out var itemIndex))
            {
                var itemLabel = metadata?.Items is not null
                    && itemIndex >= 0
                    && itemIndex < metadata.Items.Count
                    ? metadata.Items[itemIndex]
                    : null;

                result.Question = string.IsNullOrWhiteSpace(itemLabel)
                    ? $"Item {itemIndex + 1}"
                    : itemLabel;
            }
            else
            {
                result.Question = TryGetStringValue(root, "label") ?? "-";
            }
        }
        catch
        {
            // Ignore malformed payload and return defaults
        }

        return result;
    }

    private static bool TryGetPropertyCaseInsensitive(JsonElement root, string propertyName, out JsonElement value)
    {
        foreach (var property in root.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static string? TryGetStringValue(JsonElement root, string propertyName)
    {
        if (!TryGetPropertyCaseInsensitive(root, propertyName, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetDouble().ToString("0.##", CultureInfo.InvariantCulture),
            _ => null
        };
    }

    private static string GetDisplayValue(JsonElement root, string preferredPropertyName, string fallbackPropertyName)
    {
        var preferred = TryGetStringValue(root, preferredPropertyName);
        if (!string.IsNullOrWhiteSpace(preferred))
        {
            return preferred;
        }

        var fallback = TryGetStringValue(root, fallbackPropertyName);
        if (!string.IsNullOrWhiteSpace(fallback))
        {
            return fallback;
        }

        return "-";
    }

    private static bool TryGetNumericValue(
        JsonElement root,
        string preferredPropertyName,
        string fallbackPropertyName,
        out double value)
    {
        if (TryGetPropertyCaseInsensitive(root, preferredPropertyName, out var preferred)
            && TryParseNumeric(preferred, out value))
        {
            return true;
        }

        if (TryGetPropertyCaseInsensitive(root, fallbackPropertyName, out var fallback)
            && TryParseNumeric(fallback, out value))
        {
            return true;
        }

        value = 0;
        return false;
    }

    private static bool TryParseNumeric(JsonElement element, out double value)
    {
        if (element.ValueKind == JsonValueKind.Number)
        {
            return element.TryGetDouble(out value);
        }

        if (element.ValueKind == JsonValueKind.String)
        {
            return double.TryParse(
                element.GetString(),
                NumberStyles.Float | NumberStyles.AllowThousands,
                CultureInfo.InvariantCulture,
                out value);
        }

        value = 0;
        return false;
    }

    private sealed class QuadrantReportMetadata
    {
        public string XAxisLabel { get; set; } = "Complexity";
        public string YAxisLabel { get; set; } = "Effort";
        public IReadOnlyList<string> Items { get; set; } = [];
    }

    private sealed class QuadrantResponseDetails
    {
        public string Question { get; set; } = "-";
        public string XValue { get; set; } = "-";
        public string YValue { get; set; } = "-";
    }

    private sealed class AiSummaryReportDetails
    {
        public string Summary { get; set; } = string.Empty;
        public DateTimeOffset? GeneratedAt { get; set; }
    }
}
