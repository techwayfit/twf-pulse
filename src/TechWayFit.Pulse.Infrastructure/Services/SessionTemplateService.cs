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
                ? SerializeActivityConfig(activityConfig.Type, activityConfig.Config)
                : null;

            await _activityService.AddActivityAsync(
                session.Id,
                activityConfig.Order,
                activityConfig.Type,
                activityConfig.Title,
                activityConfig.Prompt,
                activityConfigJson,
                activityConfig.DurationMinutes,
                cancellationToken);
        }

        _logger.LogInformation("Created session {SessionCode} from template {TemplateId}", session.Code, templateId);

        return session;
    }

    public async Task ApplyActivitySetToSessionAsync(
        Guid templateId,
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var config = await GetTemplateConfigAsync(templateId, cancellationToken);
        if (config == null)
        {
            throw new InvalidOperationException($"Template {templateId} not found or has invalid configuration");
        }

        // Add activities from template to existing session
        foreach (var activityConfig in config.Activities.OrderBy(a => a.Order))
        {
            var activityConfigJson = activityConfig.Config != null
                ? SerializeActivityConfig(activityConfig.Type, activityConfig.Config)
                : null;

            await _activityService.AddActivityAsync(
                sessionId,
                activityConfig.Order,
                activityConfig.Type,
                activityConfig.Title,
                activityConfig.Prompt,
                activityConfigJson,
                activityConfig.DurationMinutes,
                cancellationToken);
        }

        _logger.LogInformation("Applied activity set from template {TemplateId} to session {SessionId}", templateId, sessionId);
    }

    public async Task InitializeSystemTemplatesAsync(CancellationToken cancellationToken = default)
    {
        await InitializeSystemTemplatesAsync(null, cancellationToken);
    }

    public async Task InitializeSystemTemplatesAsync(string? templatesPath, CancellationToken cancellationToken = default)
    {
        // Default path if not provided
        templatesPath ??= Path.Combine(AppContext.BaseDirectory, "App_Data", "Templates");
        var installedPath = Path.Combine(templatesPath, "installed");

        // Ensure directories exist
        Directory.CreateDirectory(templatesPath);
        Directory.CreateDirectory(installedPath);

        // Get all JSON template files
        var templateFiles = Directory.GetFiles(templatesPath, "*.json", SearchOption.TopDirectoryOnly);
        
        if (templateFiles.Length == 0)
        {
            _logger.LogInformation("No template files found in {Path}", templatesPath);
            return;
        }

        _logger.LogInformation("Found {Count} template file(s) in {Path}", templateFiles.Length, templatesPath);

        var processedCount = 0;
        var errorCount = 0;

        foreach (var filePath in templateFiles)
        {
            try
            {
                var fileName = Path.GetFileName(filePath);
                _logger.LogInformation("Processing template file: {FileName}", fileName);

                // Read and deserialize JSON
                var jsonContent = await File.ReadAllTextAsync(filePath, cancellationToken);
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
                };
                
                var templateData = JsonSerializer.Deserialize<SystemTemplateDefinition>(jsonContent, jsonOptions);

                if (templateData == null)
                {
                    _logger.LogWarning("Failed to deserialize template file: {FileName}", fileName);
                    errorCount++;
                    continue;
                }

                // Parse category
                if (!Enum.TryParse<TemplateCategory>(templateData.Category, true, out var category))
                {
                    _logger.LogWarning("Invalid category '{Category}' in template file: {FileName}", templateData.Category, fileName);
                    errorCount++;
                    continue;
                }

                // Check if template already exists (by name)
                var existingTemplates = await _templateRepository.GetSystemTemplatesAsync(cancellationToken);
                var existingTemplate = existingTemplates.FirstOrDefault(t => t.Name == templateData.Name);

                if (existingTemplate != null)
                {
                    _logger.LogInformation("Template '{Name}' already exists, updating...", templateData.Name);
                    
                    // Update existing template
                    var configJson = JsonSerializer.Serialize(templateData.Config, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });

                    existingTemplate.Update(
                        templateData.Name,
                        templateData.Description,
                        category,
                        templateData.IconEmoji,
                        configJson);

                    await _templateRepository.UpdateAsync(existingTemplate, cancellationToken);
                }
                else
                {
                    _logger.LogInformation("Creating new template: {Name}", templateData.Name);
                    
                    // Create new template
                    var configJson = JsonSerializer.Serialize(templateData.Config, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });

                    var template = new SessionTemplate(
                        Guid.NewGuid(),
                        templateData.Name,
                        templateData.Description,
                        category,
                        templateData.IconEmoji,
                        configJson,
                        isSystemTemplate: true,
                        createdByUserId: null,
                        DateTimeOffset.UtcNow,
                        DateTimeOffset.UtcNow);

                    await _templateRepository.AddAsync(template, cancellationToken);
                }

                processedCount++;

                // Move file to installed folder
                var destinationPath = Path.Combine(installedPath, fileName);
                
                // If file already exists in installed folder, delete it first
                if (File.Exists(destinationPath))
                {
                    File.Delete(destinationPath);
                }

                File.Move(filePath, destinationPath);
                _logger.LogInformation("Moved template file to installed folder: {FileName}", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing template file: {FilePath}", filePath);
                errorCount++;
            }
        }

        _logger.LogInformation(
            "Template initialization complete. Processed: {Processed}, Errors: {Errors}", 
            processedCount, 
            errorCount);
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

    private static string SerializeActivityConfig(ActivityType activityType, object config)
    {
        // All activity configs now use proper object structure in templates
        // No transformation needed - just serialize as-is with camelCase
        return JsonSerializer.Serialize(config, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });
    }

    private static List<(string Name, string Description, TemplateCategory Category, string IconEmoji, SessionTemplateConfig Config)> GetSystemTemplateConfigs()
    {
        // DEPRECATED: System templates are now loaded from JSON files in App_Data/Templates
        // This method is kept for backwards compatibility but should not be used
        return new List<(string, string, TemplateCategory, string, SessionTemplateConfig)>();
    }
}

/// <summary>
/// Definition for a system template loaded from JSON file
/// </summary>
internal sealed class SystemTemplateDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string IconEmoji { get; set; } = string.Empty;
    public SessionTemplateConfig Config { get; set; } = new();
}
