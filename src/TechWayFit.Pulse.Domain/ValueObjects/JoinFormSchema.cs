namespace TechWayFit.Pulse.Domain.ValueObjects;

public sealed class JoinFormSchema
{
    public JoinFormSchema(int maxFields, IReadOnlyList<JoinFormField> fields)
    {
        if (maxFields <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxFields), "Max fields must be greater than zero.");
        }

        if (fields.Count > maxFields)
        {
            throw new ArgumentException("Join form exceeds the configured max fields.", nameof(fields));
        }

        MaxFields = maxFields;
        Fields = fields;
    }

    public int MaxFields { get; }

    public IReadOnlyList<JoinFormField> Fields { get; }
}
