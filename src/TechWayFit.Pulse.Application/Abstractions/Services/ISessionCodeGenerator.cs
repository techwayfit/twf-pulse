namespace TechWayFit.Pulse.Application.Abstractions.Services;

/// <summary>
/// Service for generating unique session codes
/// </summary>
public interface ISessionCodeGenerator
{
    /// <summary>
    /// Generate a unique session code in XXX-XXX-XXX format
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A unique session code</returns>
    Task<string> GenerateUniqueCodeAsync(CancellationToken cancellationToken = default);
}
