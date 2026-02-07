using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TechWayFit.Pulse.Application.Abstractions.Services;
using TechWayFit.Pulse.Contracts.Requests;
using TechWayFit.Pulse.Contracts.Responses;
using TechWayFit.Pulse.Web.Api;
using TechWayFit.Pulse.Web.Components.Facilitator;
using TechWayFit.Pulse.Web.Configuration;
using TechWayFit.Pulse.Web.Services;

namespace TechWayFit.Pulse.Web.Pages.Facilitator
{
    public partial class CreateWorkshop
    {
        [Inject] private NavigationManager Navigation { get; set; } = default!;
        [Inject] private ISessionService SessionService { get; set; } = default!;
        [Inject] private IActivityService ActivityService { get; set; } = default!;
        [Inject] private ISessionCodeGenerator CodeGenerator { get; set; } = default!;
        [Inject] private ISessionAIService SessionAI { get; set; } = default!;
        [Inject] private IClientTokenService TokenService { get; set; } = default!;
        [Inject] private IOptions<ContextDocumentLimitsOptions> ContextLimits { get; set; } = default!;
        [Inject] private ILogger<CreateWorkshop> Logger { get; set; } = default!;
        [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
        [Inject] private IAuthenticationService AuthService { get; set; } = default!;

        private string sessionTitle = "Ops Pain Points Workshop — Group 1";
        private string sessionGoal = "Identify bottlenecks and quantify impact so we can prioritize fixes.";
        private string workshopType = "Ops / Process Improvement";
        private List<AgendaActivityResponse> generatedActivities = new();
        private int? editingIndex = null;
        private UpdateActivityRequest editModel = new();
        private bool isSaving = false;
        private bool isGenerating = false;
        
        // Session Schedule Fields
        private DateTime? sessionStart = null;
        private DateTime? sessionEnd = null;

        // Join Form Fields
        private List<JoinFormField> joinFormFields = new();

        // Error message
        private string? errorMessage = null;

        // Enhanced AI generation fields
        private bool showAdvancedAI = false;
        private int? aiDurationMinutes = 60;
        private int? aiParticipantCount = null;
        private string aiParticipantType = "";
        private double aiTemperature = 0.7;
        private string lastGenerationInfo = "";

        // Sprint Backlog
        private bool enableSprintBacklog = false;
        private string? sprintBacklogSummary = "";
        private string? sprintBacklogItems = "";

        // Incident Report
        private bool enableIncident = false;
        private string? incidentSummary = "";
        private string? incidentSeverity = "";
        private int? incidentDurationMinutes = null;
        private string? incidentImpactedSystems = "";

        // Product Documentation
        private bool enableProductDocs = false;
        private string? productSummary = "";
        private string? productFeatures = "";

        private EditActivityModal? editActivityModal;

        private async Task GenerateWithAI()
        {
            if (isGenerating) return;

            // Validation
            if (string.IsNullOrWhiteSpace(sessionTitle))
            {
                lastGenerationInfo = "❌ Session title is required";
                return;
            }
            if (string.IsNullOrWhiteSpace(sessionGoal))
            {
                lastGenerationInfo = "❌ Goal is required";
                return;
            }

            // Validate advanced options if enabled
            if (showAdvancedAI)
            {
                if (aiDurationMinutes.HasValue && (aiDurationMinutes < 15 || aiDurationMinutes > 600))
                {
                    lastGenerationInfo = "❌ Duration must be between 15-600 minutes";
                    return;
                }
                if (aiParticipantCount.HasValue && (aiParticipantCount < 1 || aiParticipantCount > 1000))
                {
                    lastGenerationInfo = "❌ Participant count must be between 1-1000";
                    return;
                }
                if (aiTemperature < 0 || aiTemperature > 1)
                {
                    lastGenerationInfo = "❌ Temperature must be between 0.0-1.0";
                    return;
                }

                // Validate context document summaries
                if (enableSprintBacklog && !string.IsNullOrEmpty(sprintBacklogSummary) && sprintBacklogSummary.Length > ContextLimits.Value.SprintBacklogSummaryMaxChars)
                {
                    lastGenerationInfo = $"❌ Sprint backlog summary must be {ContextLimits.Value.SprintBacklogSummaryMaxChars} characters or less";
                    return;
                }
                if (enableIncident && !string.IsNullOrEmpty(incidentSummary) && incidentSummary.Length > ContextLimits.Value.IncidentSummaryMaxChars)
                {
                    lastGenerationInfo = $"❌ Incident summary must be {ContextLimits.Value.IncidentSummaryMaxChars} characters or less";
                    return;
                }
                if (enableProductDocs && !string.IsNullOrEmpty(productSummary) && productSummary.Length > ContextLimits.Value.ProductSummaryMaxChars)
                {
                    lastGenerationInfo = $"❌ Product summary must be {ContextLimits.Value.ProductSummaryMaxChars} characters or less";
                    return;
                }
            }

            isGenerating = true;
            lastGenerationInfo = "";

            try
            {
                var request = new CreateSessionRequest
                {
                    Title = sessionTitle,
                    Goal = sessionGoal,
                    Settings = new Contracts.Models.SessionSettingsDto { StrictCurrentActivityOnly = false, AllowAnonymous = false, TtlMinutes = 360 },
                    SessionStart = sessionStart,
                    SessionEnd = sessionEnd
                };

                // Build enhanced context if advanced options are used
                if (showAdvancedAI && (aiDurationMinutes.HasValue || !string.IsNullOrEmpty(aiParticipantType) ||
                    enableSprintBacklog || enableIncident || enableProductDocs))
                {
                    var context = new Contracts.Models.SessionGenerationContextDto
                    {
                        WorkshopType = workshopType,
                        DurationMinutes = aiDurationMinutes,
                        ParticipantCount = aiParticipantCount
                    };

                    // Participant types
                    if (!string.IsNullOrEmpty(aiParticipantType))
                    {
                        context.ParticipantTypes = new Contracts.Models.ParticipantTypesDto
                        {
                            Primary = aiParticipantType
                        };
                    }

                    // Context documents
                    var docs = new Contracts.Models.ContextDocumentsDto();
                    bool hasAnyDoc = false;

                    if (enableSprintBacklog && !string.IsNullOrEmpty(sprintBacklogSummary))
                    {
                        hasAnyDoc = true;
                        docs.SprintBacklog = new Contracts.Models.SprintBacklogDto
                        {
                            Provided = true,
                            Summary = sprintBacklogSummary
                        };
                        if (!string.IsNullOrEmpty(sprintBacklogItems))
                        {
                            docs.SprintBacklog.KeyItems = sprintBacklogItems.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
                        }
                    }

                    if (enableIncident && !string.IsNullOrEmpty(incidentSummary))
                    {
                        hasAnyDoc = true;
                        docs.IncidentReport = new Contracts.Models.IncidentReportDto
                        {
                            Provided = true,
                            Summary = incidentSummary,
                            Severity = incidentSeverity,
                            DurationMinutes = incidentDurationMinutes
                        };
                        if (!string.IsNullOrEmpty(incidentImpactedSystems))
                        {
                            docs.IncidentReport.ImpactedSystems = incidentImpactedSystems.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
                        }
                    }

                    if (enableProductDocs && !string.IsNullOrEmpty(productSummary))
                    {
                        hasAnyDoc = true;
                        docs.ProductDocumentation = new Contracts.Models.ProductDocumentationDto
                        {
                            Provided = true,
                            Summary = productSummary
                        };
                        if (!string.IsNullOrEmpty(productFeatures))
                        {
                            docs.ProductDocumentation.Features = productFeatures.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
                        }
                    }

                    if (hasAnyDoc)
                    {
                        context.ContextDocuments = docs;
                    }

                    request.GenerationContext = context;

                    // Generation options
                    request.GenerationOptions = new Contracts.Models.SessionGenerationOptionsDto
                    {
                        Temperature = aiTemperature
                    };
                }
                else
                {
                    // Legacy path - use Context string
                    request.Context = workshopType;
                }

                var result = await SessionAI.GenerateSessionActivitiesAsync(request, CancellationToken.None);
                generatedActivities = result.ToList();

                lastGenerationInfo = $"✓ Generated {generatedActivities.Count} activities";
            }
            catch (Exception ex)
            {
                lastGenerationInfo = $"❌ Failed to generate: {ex.Message}";
            }
            finally
            {
                isGenerating = false;
            }
        }

        private void EditGeneratedActivity(int index)
        {
            // Deprecated - use modal
        }

        private void ShowEditModal(int index)
        {
            if (index < 0 || index >= generatedActivities.Count) return;
            var a = generatedActivities[index];
            if (editActivityModal != null)
            {
                editActivityModal.SessionCode = null; // draft mode
                editActivityModal.Activity = a;
                editActivityModal.Show();
            }
        }

        private void CancelGeneratedEdit()
        {
            editingIndex = null;
            editModel = new UpdateActivityRequest();
        }

        private void SaveGeneratedEdit()
        {
            // Deprecated - handled by modal
        }

        private void RemoveGeneratedActivity(int index)
        {
            if (index < 0 || index >= generatedActivities.Count) return;
            generatedActivities.RemoveAt(index);
            // Reindex orders
            for (int i = 0; i < generatedActivities.Count; i++)
            {
                var a = generatedActivities[i];
                generatedActivities[i] = new AgendaActivityResponse(a.ActivityId, i + 1, a.Type, a.Title, a.Prompt, a.Config, a.Status, a.OpenedAt, a.ClosedAt, a.DurationMinutes);
            }
        }

        private Task HandleDraftActivityUpdated(AgendaActivityResponse updated)
        {
            var idx = generatedActivities.FindIndex(a => a.ActivityId == updated.ActivityId);
            if (idx >= 0)
            {
                generatedActivities[idx] = updated;
            }
            return Task.CompletedTask;
        }

        private Task HandleDraftActivityDeleted(Guid activityId)
        {
            var idx = generatedActivities.FindIndex(a => a.ActivityId == activityId);
            if (idx >= 0)
            {
                generatedActivities.RemoveAt(idx);
                // reindex
                for (int i = 0; i < generatedActivities.Count; i++)
                {
                    var a = generatedActivities[i];
                    generatedActivities[i] = new AgendaActivityResponse(a.ActivityId, i + 1, a.Type, a.Title, a.Prompt, a.Config, a.Status, a.OpenedAt, a.ClosedAt, a.DurationMinutes);
                }
            }
            return Task.CompletedTask;
        }

        private async Task CreateAndSaveSession()
        {
            if (isSaving) return;
            isSaving = true;
            errorMessage = null; // Clear any previous error
            try
            {
                // Get authenticated facilitator ID
                var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
                var user = authState.User;

                Guid? facilitatorUserId = null;
                if (user?.Identity?.IsAuthenticated == true)
                {
                    var userIdClaim = user.FindFirst("FacilitatorUserId")?.Value;
                    if (Guid.TryParse(userIdClaim, out var userId))
                    {
                        facilitatorUserId = userId;
                    }
                    else
                    {
                        var email = user.FindFirst(ClaimTypes.Email)?.Value;
                        if (!string.IsNullOrWhiteSpace(email))
                        {
                            var facilitator = await AuthService.GetFacilitatorByEmailAsync(email);
                            facilitatorUserId = facilitator?.Id;
                        }
                    }
                }

                if (!facilitatorUserId.HasValue)
                {
                    errorMessage = "You must be logged in as a facilitator to create sessions.";
                    return;
                }

                // Validate join form fields before submission
                foreach (var field in joinFormFields)
                {
                    if (field.Type == "dropdown" && string.IsNullOrWhiteSpace(field.Options))
                    {
                        errorMessage = $"Dropdown field '{field.Label}' must have options defined (comma-separated values).";
                        return;
                    }
                }

                // Build join form schema from join form fields
                var joinFormFieldDtos = joinFormFields.Select((f, index) => new Contracts.Models.JoinFormFieldDto
                {
                    Id = $"field_{index + 1}",
                    Label = f.Label,
                    Type = f.Type switch
                    {
                        "dropdown" => Contracts.Enums.FieldType.Dropdown,
                        "multiselect" => Contracts.Enums.FieldType.MultiSelect,
                        "text" => Contracts.Enums.FieldType.Text,
                        "number" => Contracts.Enums.FieldType.Number,
                        "boolean" => Contracts.Enums.FieldType.Boolean,
                        _ => Contracts.Enums.FieldType.Text
                    },
                    Required = false,
                    Options = f.Options ?? string.Empty,
                    UseInFilters = false
                }).ToList();

                var settingsDto = new Contracts.Models.SessionSettingsDto { StrictCurrentActivityOnly = false, AllowAnonymous = false, TtlMinutes = 360 };
                var joinFormSchemaDto = new Contracts.Models.JoinFormSchemaDto
                {
                    MaxFields = 5,
                    Fields = joinFormFieldDtos
                };

                // Convert DTOs to domain models using ApiMapper
                var settings = ApiMapper.ToDomain(settingsDto);
                var joinFormSchema = ApiMapper.ToDomain(joinFormSchemaDto);

                // Generate unique code
                var code = await CodeGenerator.GenerateUniqueCodeAsync(CancellationToken.None);

                // Create session directly using service
                var created = await SessionService.CreateSessionAsync(
                    code,
                    sessionTitle,
                    sessionGoal,
                    workshopType,
                    settings,
                    joinFormSchema,
                    DateTimeOffset.UtcNow,
                    facilitatorUserId,
                    null, // groupId
                    CancellationToken.None);

                // Create activities directly using service
                foreach (var a in generatedActivities.OrderBy(x => x.Order))
                {
                    await ActivityService.AddActivityAsync(
                        created.Id,
                        a.Order,
                        ApiMapper.MapActivityType(a.Type),
                        a.Title,
                        a.Prompt,
                        a.Config,
                        a.DurationMinutes,
                        CancellationToken.None);
                }

                // Get facilitator token for the live view
                string? token = null;
                try
                {
                    token = await TokenService.GetFacilitatorTokenAsync(created.Code);
                }
                catch
                {
                    // Token will be obtained when navigating to live view
                }

                // Navigate to facilitator live view for the created session
                Navigation.NavigateTo($"/facilitator/live?Code={created.Code}");
            }
            catch (HttpRequestException ex)
            {
                // Extract detailed error message from the exception
                var errorDetail = ex.Message;

                // Try to extract just the validation error if it's a 400 error
                if (errorDetail.Contains("400") && errorDetail.Contains("-"))
                {
                    var parts = errorDetail.Split(new[] { " - " }, 2, StringSplitOptions.None);
                    if (parts.Length > 1)
                    {
                        errorDetail = parts[1]; // Get the part after "400 - "
                    }
                }

                errorMessage = $"Failed to save session: {errorDetail}";
                Logger.LogError(ex, "Error saving session");
            }
            catch (InvalidOperationException ex)
            {
                errorMessage = $"Configuration error: {ex.Message}";
                Logger.LogError(ex, "Configuration error while saving session");
            }
            catch (Exception ex)
            {
                errorMessage = $"An unexpected error occurred: {ex.Message}";
                Logger.LogError(ex, "Unexpected error while saving session");
            }
            finally
            {
                isSaving = false;
            }
        }

        // Join Form Field Management
        private void AddJoinFormField()
        {
            if (joinFormFields.Count < 5)
            {
                joinFormFields.Add(new JoinFormField
                {
                    Label = $"Field {joinFormFields.Count + 1}",
                    Type = "dropdown"
                });
            }
        }

        private void RemoveJoinFormField(JoinFormField field)
        {
            joinFormFields.Remove(field);
        }

        private class JoinFormField
        {
            public string Label { get; set; } = string.Empty;
            public string Type { get; set; } = "dropdown";
            public string Options { get; set; } = string.Empty;
        }
    }
}
