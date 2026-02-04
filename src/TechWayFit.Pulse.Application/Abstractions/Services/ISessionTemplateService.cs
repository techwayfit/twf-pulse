using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Domain.Enums;
using TechWayFit.Pulse.Domain.Models;

namespace TechWayFit.Pulse.Application.Abstractions.Services;

public interface ISessionTemplateService
{
    /// <summary>
    /// Get all available templates
    /// </summary>
    Task<IReadOnlyList<SessionTemplate>> GetAllTemplatesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get templates by category
    /// </summary>
    Task<IReadOnlyList<SessionTemplate>> GetTemplatesByCategoryAsync(TemplateCategory category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get template by ID
    /// </summary>
    Task<SessionTemplate?> GetTemplateByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get parsed template configuration
    /// </summary>
    Task<SessionTemplateConfig?> GetTemplateConfigAsync(Guid templateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new custom template
    /// </summary>
    Task<SessionTemplate> CreateTemplateAsync(
        string name,
        string description,
        TemplateCategory category,
        string iconEmoji,
        SessionTemplateConfig config,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing template (only user-created templates)
    /// </summary>
    Task UpdateTemplateAsync(
        Guid templateId,
        string name,
        string description,
        TemplateCategory category,
        string iconEmoji,
        SessionTemplateConfig config,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a template (only user-created templates)
    /// </summary>
    Task DeleteTemplateAsync(Guid templateId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Apply activity set from template to an existing session
    /// This adds only activities from the template, not session metadata
    /// </summary>
    Task ApplyActivitySetToSessionAsync(
        Guid templateId,
        Guid sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a session from a template with optional customizations
    /// </summary>
    Task<Session> CreateSessionFromTemplateAsync(
        Guid templateId,
        Guid facilitatorUserId,
        Guid? groupId,
        SessionTemplateConfig? customizations,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Initialize system templates on startup from JSON files in App_Data/Templates
    /// </summary>
    Task InitializeSystemTemplatesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Initialize system templates from JSON files at specified path
    /// </summary>
    Task InitializeSystemTemplatesAsync(string? templatesPath, CancellationToken cancellationToken = default);
}
