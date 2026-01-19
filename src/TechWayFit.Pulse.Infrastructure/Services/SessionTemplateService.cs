using System.Text.Json;
using Microsoft.Extensions.Logging;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Application.Abstractions.Services;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Domain.Enums;
using TechWayFit.Pulse.Domain.Models;
using TechWayFit.Pulse.Domain.ValueObjects;

namespace TechWayFit.Pulse.Infrastructure.Services;

public sealed class SessionTemplateService : ISessionTemplateService
{
    private readonly ISessionTemplateRepository _templateRepository;
    private readonly ISessionService _sessionService;
    private readonly IActivityService _activityService;
    private readonly ISessionCodeGenerator _codeGenerator;
    private readonly ILogger<SessionTemplateService> _logger;

    public SessionTemplateService(
        ISessionTemplateRepository templateRepository,
        ISessionService sessionService,
        IActivityService activityService,
        ISessionCodeGenerator codeGenerator,
        ILogger<SessionTemplateService> logger)
    {
        _templateRepository = templateRepository;
        _sessionService = sessionService;
        _activityService = activityService;
        _codeGenerator = codeGenerator;
        _logger = logger;
    }

    public Task<IReadOnlyList<SessionTemplate>> GetAllTemplatesAsync(CancellationToken cancellationToken = default)
    {
        return _templateRepository.GetAllAsync(cancellationToken);
    }

    public Task<IReadOnlyList<SessionTemplate>> GetTemplatesByCategoryAsync(TemplateCategory category, CancellationToken cancellationToken = default)
    {
        return _templateRepository.GetByCategoryAsync(category, cancellationToken);
    }

    public Task<SessionTemplate?> GetTemplateByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _templateRepository.GetByIdAsync(id, cancellationToken);
    }

    public async Task<SessionTemplateConfig?> GetTemplateConfigAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        var template = await _templateRepository.GetByIdAsync(templateId, cancellationToken);
        if (template == null)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<SessionTemplateConfig>(template.ConfigJson);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize template config for template {TemplateId}", templateId);
            return null;
        }
    }

    public async Task<SessionTemplate> CreateTemplateAsync(
        string name,
        string description,
        TemplateCategory category,
        string iconEmoji,
        SessionTemplateConfig config,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var configJson = JsonSerializer.Serialize(config, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        var template = new SessionTemplate(
            Guid.NewGuid(),
            name,
            description,
            category,
            iconEmoji,
            configJson,
            isSystemTemplate: false,
            createdByUserId: userId,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);

        await _templateRepository.AddAsync(template, cancellationToken);

        _logger.LogInformation("Created template {TemplateId} by user {UserId}", template.Id, userId);

        return template;
    }

    public async Task UpdateTemplateAsync(
        Guid templateId,
        string name,
        string description,
        TemplateCategory category,
        string iconEmoji,
        SessionTemplateConfig config,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var template = await _templateRepository.GetByIdAsync(templateId, cancellationToken);
        if (template == null)
        {
            throw new InvalidOperationException($"Template {templateId} not found");
        }

        if (template.IsSystemTemplate)
        {
            throw new InvalidOperationException("Cannot update system templates");
        }

        if (template.CreatedByUserId != userId)
        {
            throw new UnauthorizedAccessException("Cannot update template created by another user");
        }

        var configJson = JsonSerializer.Serialize(config, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        template.Update(name, description, category, iconEmoji, configJson);

        await _templateRepository.UpdateAsync(template, cancellationToken);

        _logger.LogInformation("Updated template {TemplateId} by user {UserId}", templateId, userId);
    }

    public async Task DeleteTemplateAsync(Guid templateId, Guid userId, CancellationToken cancellationToken = default)
    {
        var template = await _templateRepository.GetByIdAsync(templateId, cancellationToken);
        if (template == null)
        {
            throw new InvalidOperationException($"Template {templateId} not found");
        }

        if (template.IsSystemTemplate)
        {
            throw new InvalidOperationException("Cannot delete system templates");
        }

        if (template.CreatedByUserId != userId)
        {
            throw new UnauthorizedAccessException("Cannot delete template created by another user");
        }

        await _templateRepository.DeleteAsync(templateId, cancellationToken);

        _logger.LogInformation("Deleted template {TemplateId} by user {UserId}", templateId, userId);
    }

    public async Task<Session> CreateSessionFromTemplateAsync(
        Guid templateId,
        Guid facilitatorUserId,
        Guid? groupId,
        SessionTemplateConfig? customizations,
        CancellationToken cancellationToken = default)
    {
        var config = await GetTemplateConfigAsync(templateId, cancellationToken);
        if (config == null)
        {
            throw new InvalidOperationException($"Template {templateId} not found or has invalid configuration");
        }

        // Apply customizations if provided
        if (customizations != null)
        {
            if (!string.IsNullOrWhiteSpace(customizations.Title))
                config.Title = customizations.Title;
            
            if (customizations.Goal != null)
                config.Goal = customizations.Goal;
            
            if (customizations.Context != null)
                config.Context = customizations.Context;

            if (customizations.Settings != null)
                MergeSettings(config.Settings, customizations.Settings);

            if (customizations.JoinFormSchema != null)
                MergeJoinFormSchema(config.JoinFormSchema, customizations.JoinFormSchema);
        }

        // Create session settings with default values
        var sessionSettings = new SessionSettings(
            maxContributionsPerParticipantPerSession: 100,
            maxContributionsPerParticipantPerActivity: config.Settings.MaxParticipants ?? null,
            strictCurrentActivityOnly: true,
            allowAnonymous: config.Settings.AllowAnonymous,
            ttlMinutes: config.Settings.DurationMinutes ?? 120);

        // Create join form schema
        var maxFields = 10; // Default max fields
        var joinFormFields = config.JoinFormSchema.Fields.Select(f =>
        {
            var fieldType = f.Type.ToLowerInvariant() switch
            {
                "text" => FieldType.Text,
                "number" => FieldType.Number,
                "select" or "dropdown" => FieldType.Dropdown,
                "multiselect" => FieldType.MultiSelect,
                "boolean" or "checkbox" => FieldType.Boolean,
                _ => FieldType.Text
            };

            return new JoinFormField(
                f.Name,
                f.Label,
                fieldType,
                f.Required,
                f.Options ?? new List<string>(),
                useInFilters: false);
        }).ToList();

        var joinFormSchema = new JoinFormSchema(maxFields, joinFormFields);

        // Generate session code
        var sessionCode = await _codeGenerator.GenerateUniqueCodeAsync(cancellationToken);

        // Create session
        var session = await _sessionService.CreateSessionAsync(
            sessionCode,
            config.Title,
            config.Goal,
            config.Context,
            sessionSettings,
            joinFormSchema,
            DateTimeOffset.UtcNow,
            facilitatorUserId,
            groupId,
            cancellationToken);

        // Create activities
        foreach (var activityConfig in config.Activities.OrderBy(a => a.Order))
        {
            var activityConfigJson = activityConfig.Config != null
                ? JsonSerializer.Serialize(activityConfig.Config)
                : null;

            await _activityService.AddActivityAsync(
                session.Id,
                activityConfig.Order,
                activityConfig.Type,
                activityConfig.Title,
                activityConfig.Prompt,
                activityConfigJson,
                cancellationToken);
        }

        _logger.LogInformation("Created session {SessionCode} from template {TemplateId}", session.Code, templateId);

        return session;
    }

    public async Task InitializeSystemTemplatesAsync(CancellationToken cancellationToken = default)
    {
        var existingTemplates = await _templateRepository.GetSystemTemplatesAsync(cancellationToken);
        if (existingTemplates.Any())
        {
            _logger.LogInformation("System templates already initialized ({Count} templates)", existingTemplates.Count);
            return;
        }

        _logger.LogInformation("Initializing system templates...");

        var templates = GetSystemTemplateConfigs();

        foreach (var (name, description, category, iconEmoji, config) in templates)
        {
            var configJson = JsonSerializer.Serialize(config, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            var template = new SessionTemplate(
                Guid.NewGuid(),
                name,
                description,
                category,
                iconEmoji,
                configJson,
                isSystemTemplate: true,
                createdByUserId: null,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow);

            await _templateRepository.AddAsync(template, cancellationToken);
        }

        _logger.LogInformation("Initialized {Count} system templates", templates.Count);
    }

    private static void MergeSettings(SessionSettingsConfig target, SessionSettingsConfig source)
    {
        if (source.DurationMinutes.HasValue)
            target.DurationMinutes = source.DurationMinutes;

        if (source.MaxParticipants.HasValue)
            target.MaxParticipants = source.MaxParticipants;

        target.AllowAnonymous = source.AllowAnonymous;
        target.AllowLateJoin = source.AllowLateJoin;
        target.ShowResultsDuringActivity = source.ShowResultsDuringActivity;
    }

    private static void MergeJoinFormSchema(JoinFormSchemaConfig target, JoinFormSchemaConfig source)
    {
        if (source.Fields != null && source.Fields.Any())
        {
            target.Fields = source.Fields;
        }
    }

    private static List<(string Name, string Description, TemplateCategory Category, string IconEmoji, SessionTemplateConfig Config)> GetSystemTemplateConfigs()
    {
        return new List<(string, string, TemplateCategory, string, SessionTemplateConfig)>
        {
            // Retro Sprint Review
            ("Retro Sprint Review", "Quick pulse + themes + actions", TemplateCategory.Retrospective, "🔄", new SessionTemplateConfig
            {
                Title = "Sprint Retrospective",
                Goal = "Reflect on the sprint and identify improvements",
                Context = "Team retrospective for sprint review",
                Settings = new SessionSettingsConfig
                {
                    DurationMinutes = 60,
                    AllowAnonymous = false,
                    AllowLateJoin = true,
                    ShowResultsDuringActivity = true
                },
                JoinFormSchema = new JoinFormSchemaConfig
                {
                    Fields = new List<JoinFormFieldConfig>
                    {
                        new() { Name = "name", Label = "Your Name", Type = "text", Required = true },
                        new() { Name = "role", Label = "Role", Type = "select", Required = true, Options = new List<string> { "Developer", "Designer", "QA", "Product Owner", "Scrum Master" } }
                    }
                },
                Activities = new List<ActivityTemplateConfig>
                {
                    new() { Order = 1, Type = ActivityType.WordCloud, Title = "Sprint in One Word", Prompt = "Describe this sprint in one word" },
                    new() { Order = 2, Type = ActivityType.Poll, Title = "Sprint Satisfaction", Prompt = "How satisfied are you with this sprint?", Config = new ActivityConfigData
                    {
                        Options = new List<string> { "😞 Very Unsatisfied", "😐 Unsatisfied", "🙂 Neutral", "😊 Satisfied", "🎉 Very Satisfied" }
                    }},
                    new() { Order = 3, Type = ActivityType.Quadrant, Title = "What Went Well / What Didn't", Prompt = "Share your thoughts", Config = new ActivityConfigData
                    {
                        XAxisLabel = "Impact",
                        YAxisLabel = "Control",
                        TopLeftLabel = "High Impact, Low Control",
                        TopRightLabel = "High Impact, High Control",
                        BottomLeftLabel = "Low Impact, Low Control",
                        BottomRightLabel = "Low Impact, High Control"
                    }},
                    new() { Order = 4, Type = ActivityType.GeneralFeedback, Title = "Action Items", Prompt = "What should we do differently next sprint?", Config = new ActivityConfigData
                    {
                        Categories = new List<string> { "Process", "Collaboration", "Tools", "Quality" }
                    }}
                }
            }),

            // Ops Pain Points
            ("Ops Pain Points", "Impact/Effort + 5-Whys", TemplateCategory.IncidentReview, "⚙️", new SessionTemplateConfig
            {
                Title = "Operations Pain Points Workshop",
                Goal = "Identify and prioritize operational challenges",
                Context = "Workshop to surface and analyze operational issues",
                Settings = new SessionSettingsConfig
                {
                    DurationMinutes = 90,
                    AllowAnonymous = false,
                    AllowLateJoin = false,
                    ShowResultsDuringActivity = true
                },
                JoinFormSchema = new JoinFormSchemaConfig
                {
                    Fields = new List<JoinFormFieldConfig>
                    {
                        new() { Name = "name", Label = "Your Name", Type = "text", Required = true },
                        new() { Name = "team", Label = "Team", Type = "text", Required = true }
                    }
                },
                Activities = new List<ActivityTemplateConfig>
                {
                    new() { Order = 1, Type = ActivityType.GeneralFeedback, Title = "Pain Point Collection", Prompt = "What operational challenges are you facing?", Config = new ActivityConfigData
                    {
                        Categories = new List<string> { "Infrastructure", "Deployment", "Monitoring", "Incidents", "Toil" }
                    }},
                    new() { Order = 2, Type = ActivityType.Quadrant, Title = "Impact vs Effort Matrix", Prompt = "Plot pain points by impact and effort to fix", Config = new ActivityConfigData
                    {
                        XAxisLabel = "Effort to Fix",
                        YAxisLabel = "Business Impact",
                        TopLeftLabel = "High Impact, Low Effort (Quick Wins)",
                        TopRightLabel = "High Impact, High Effort (Major Projects)",
                        BottomLeftLabel = "Low Impact, Low Effort (Fill-ins)",
                        BottomRightLabel = "Low Impact, High Effort (Avoid)"
                    }},
                    new() { Order = 3, Type = ActivityType.FiveWhys, Title = "Root Cause Analysis", Prompt = "Let's dig into the top pain point", Config = new ActivityConfigData
                    {
                        MaxDepth = 5
                    }},
                    new() { Order = 4, Type = ActivityType.GeneralFeedback, Title = "Solutions & Next Steps", Prompt = "What actions can we take?" }
                }
            }),

            // Product Discovery
            ("Product Discovery", "Idea cloud + prioritization", TemplateCategory.ProductDiscovery, "💡", new SessionTemplateConfig
            {
                Title = "Product Discovery Session",
                Goal = "Generate and prioritize product ideas",
                Context = "Collaborative session for product ideation",
                Settings = new SessionSettingsConfig
                {
                    DurationMinutes = 120,
                    AllowAnonymous = false,
                    AllowLateJoin = true,
                    ShowResultsDuringActivity = true
                },
                JoinFormSchema = new JoinFormSchemaConfig
                {
                    Fields = new List<JoinFormFieldConfig>
                    {
                        new() { Name = "name", Label = "Your Name", Type = "text", Required = true },
                        new() { Name = "role", Label = "Role", Type = "select", Required = true, Options = new List<string> { "Product Manager", "Designer", "Engineer", "Stakeholder", "User Researcher" } }
                    }
                },
                Activities = new List<ActivityTemplateConfig>
                {
                    new() { Order = 1, Type = ActivityType.WordCloud, Title = "Customer Needs", Prompt = "What do our customers need most?" },
                    new() { Order = 2, Type = ActivityType.GeneralFeedback, Title = "Feature Ideas", Prompt = "Share your product ideas", Config = new ActivityConfigData
                    {
                        Categories = new List<string> { "New Feature", "Improvement", "Integration", "UX Enhancement" }
                    }},
                    new() { Order = 3, Type = ActivityType.Quadrant, Title = "Value vs Complexity", Prompt = "Let's prioritize these ideas", Config = new ActivityConfigData
                    {
                        XAxisLabel = "Implementation Complexity",
                        YAxisLabel = "User Value",
                        TopLeftLabel = "High Value, Low Complexity (Do First)",
                        TopRightLabel = "High Value, High Complexity (Plan)",
                        BottomLeftLabel = "Low Value, Low Complexity (Maybe)",
                        BottomRightLabel = "Low Value, High Complexity (Avoid)"
                    }},
                    new() { Order = 4, Type = ActivityType.Poll, Title = "Top Priority Vote", Prompt = "Which idea should we build first?", Config = new ActivityConfigData
                    {
                        Options = new List<string> { "Idea 1", "Idea 2", "Idea 3", "Idea 4", "Idea 5" }
                    }},
                    new() { Order = 5, Type = ActivityType.GeneralFeedback, Title = "Next Steps", Prompt = "What are the action items?" }
                }
            }),

            // Incident Review
            ("Incident Review", "Root cause ladder + fixes", TemplateCategory.IncidentReview, "🚨", new SessionTemplateConfig
            {
                Title = "Incident Post-Mortem",
                Goal = "Learn from the incident and prevent recurrence",
                Context = "Post-incident review and learning session",
                Settings = new SessionSettingsConfig
                {
                    DurationMinutes = 60,
                    AllowAnonymous = false,
                    AllowLateJoin = false,
                    ShowResultsDuringActivity = true
                },
                JoinFormSchema = new JoinFormSchemaConfig
                {
                    Fields = new List<JoinFormFieldConfig>
                    {
                        new() { Name = "name", Label = "Your Name", Type = "text", Required = true },
                        new() { Name = "role", Label = "Role", Type = "select", Required = true, Options = new List<string> { "Incident Commander", "Engineer", "SRE", "Support", "Product" } }
                    }
                },
                Activities = new List<ActivityTemplateConfig>
                {
                    new() { Order = 1, Type = ActivityType.QnA, Title = "Incident Timeline", Prompt = "Share key events during the incident" },
                    new() { Order = 2, Type = ActivityType.FiveWhys, Title = "Root Cause Analysis", Prompt = "Why did this incident occur?", Config = new ActivityConfigData
                    {
                        MaxDepth = 5
                    }},
                    new() { Order = 3, Type = ActivityType.GeneralFeedback, Title = "Contributing Factors", Prompt = "What other factors contributed?", Config = new ActivityConfigData
                    {
                        Categories = new List<string> { "Technical", "Process", "Communication", "Monitoring", "Documentation" }
                    }},
                    new() { Order = 4, Type = ActivityType.Quadrant, Title = "Remediation Prioritization", Prompt = "Prioritize fixes and improvements", Config = new ActivityConfigData
                    {
                        XAxisLabel = "Effort",
                        YAxisLabel = "Impact on Prevention",
                        TopLeftLabel = "High Impact, Low Effort (Do Now)",
                        TopRightLabel = "High Impact, High Effort (Schedule)",
                        BottomLeftLabel = "Low Impact, Low Effort (Nice to Have)",
                        BottomRightLabel = "Low Impact, High Effort (Skip)"
                    }},
                    new() { Order = 5, Type = ActivityType.GeneralFeedback, Title = "Action Items", Prompt = "What are the concrete next steps?" },
                    new() { Order = 6, Type = ActivityType.Rating, Title = "Incident Response Rating", Prompt = "How well did we respond?", Config = new ActivityConfigData
                    {
                        MaxRating = 5,
                        RatingLabel = "Response Quality"
                    }}
                }
            })
        };
    }
}
