using System.Text.Json;
using System.Text.Json.Serialization;
using TechWayFit.Pulse.Domain.Enums;
using TechWayFit.Pulse.Domain.ValueObjects;

namespace TechWayFit.Pulse.Infrastructure.Persistence;

internal static class PersistenceJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    internal static string SerializeJoinFormSchema(JoinFormSchema schema)
    {
        var dto = new JoinFormSchemaDto
        {
            MaxFields = schema.MaxFields,
            Fields = schema.Fields.Select(field => new JoinFormFieldDto
            {
                Id = field.Id,
                Label = field.Label,
                Type = field.Type,
                Required = field.Required,
                Options = field.Options.ToList(),
                UseInFilters = field.UseInFilters
            }).ToList()
        };

        return JsonSerializer.Serialize(dto, Options);
    }

    internal static JoinFormSchema DeserializeJoinFormSchema(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidOperationException("Join form schema JSON is required.");
        }

        var dto = JsonSerializer.Deserialize<JoinFormSchemaDto>(json, Options)
                  ?? throw new InvalidOperationException("Join form schema JSON is invalid.");

        var fields = dto.Fields.Select(field => new JoinFormField(
            field.Id,
            field.Label,
            field.Type,
            field.Required,
            field.Options,
            field.UseInFilters)).ToList();

        return new JoinFormSchema(dto.MaxFields, fields);
    }

    internal static string SerializeSessionSettings(SessionSettings settings)
    {
        var dto = new SessionSettingsDto
        {
            StrictCurrentActivityOnly = settings.StrictCurrentActivityOnly,
            AllowAnonymous = settings.AllowAnonymous,
            TtlMinutes = settings.TtlMinutes
        };

        return JsonSerializer.Serialize(dto, Options);
    }

    internal static SessionSettings DeserializeSessionSettings(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidOperationException("Session settings JSON is required.");
        }

        var dto = JsonSerializer.Deserialize<SessionSettingsDto>(json, Options)
                  ?? throw new InvalidOperationException("Session settings JSON is invalid.");

        return new SessionSettings( 
            dto.StrictCurrentActivityOnly,
            dto.AllowAnonymous,
            dto.TtlMinutes);
    }

    internal static string SerializeDimensions(IReadOnlyDictionary<string, string?> dimensions)
    {
        var payload = dimensions.ToDictionary(entry => entry.Key, entry => entry.Value);
        return JsonSerializer.Serialize(payload, Options);
    }

    internal static IReadOnlyDictionary<string, string?> DeserializeDimensions(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, string?>();
        }

        return JsonSerializer.Deserialize<Dictionary<string, string?>>(json, Options)
               ?? new Dictionary<string, string?>();
    }

    private sealed class JoinFormSchemaDto
    {
        public int MaxFields { get; set; }

        public List<JoinFormFieldDto> Fields { get; set; } = new();
    }

    private sealed class JoinFormFieldDto
    {
        public string Id { get; set; } = string.Empty;

        public string Label { get; set; } = string.Empty;

        public FieldType Type { get; set; }

        public bool Required { get; set; }

        public List<string> Options { get; set; } = new();

        public bool UseInFilters { get; set; }
    }

    private sealed class SessionSettingsDto
    {
        public int MaxContributionsPerParticipantPerSession { get; set; }

        public int? MaxContributionsPerParticipantPerActivity { get; set; }

        public bool StrictCurrentActivityOnly { get; set; }

        public bool AllowAnonymous { get; set; }

        public int TtlMinutes { get; set; }
    }
}
