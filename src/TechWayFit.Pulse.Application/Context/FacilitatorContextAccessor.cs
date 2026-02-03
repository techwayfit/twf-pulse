namespace TechWayFit.Pulse.Application.Context;

/// <summary>
/// Provides access to the current facilitator context using AsyncLocal storage
/// </summary>
public static class FacilitatorContextAccessor
{
    private static readonly AsyncLocal<FacilitatorContext?> _currentContext = new();

    /// <summary>
    /// Gets the current facilitator context for this async flow
    /// </summary>
    public static FacilitatorContext? Current => _currentContext.Value;

    /// <summary>
    /// Sets the current facilitator context for this async flow
    /// </summary>
    public static void Set(FacilitatorContext? context)
    {
        _currentContext.Value = context;
    }

    /// <summary>
    /// Clears the current facilitator context
    /// </summary>
    public static void Clear()
    {
        _currentContext.Value = null;
    }
}
