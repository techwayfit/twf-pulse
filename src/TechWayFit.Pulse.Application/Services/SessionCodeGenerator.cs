using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Application.Abstractions.Services;

namespace TechWayFit.Pulse.Application.Services;

/// <summary>
/// Generates unique session codes in XXX-XXX-XXX format
/// Uses alphanumeric characters excluding ambiguous ones (0, O, I, 1, etc.)
/// </summary>
public sealed class SessionCodeGenerator : ISessionCodeGenerator
{
    private readonly ISessionRepository _sessions;
    private const string AllowedChars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Excluding 0, O, I, 1 for clarity
    private const int SegmentLength = 3;
  private const int SegmentCount = 3;
    private const int MaxRetries = 10;

    public SessionCodeGenerator(ISessionRepository sessions)
    {
      _sessions = sessions;
    }

    public async Task<string> GenerateUniqueCodeAsync(CancellationToken cancellationToken = default)
    {
        for (int attempt = 0; attempt < MaxRetries; attempt++)
        {
            var code = GenerateCode();
      
    // Check if code already exists
            var existing = await _sessions.GetByCodeAsync(code, cancellationToken);
            if (existing == null)
 {
                return code;
         }
     }

        // If we've exhausted retries, throw an exception
        throw new InvalidOperationException($"Failed to generate a unique session code after {MaxRetries} attempts.");
    }

    private static string GenerateCode()
    {
        var segments = new string[SegmentCount];
   
        for (int i = 0; i < SegmentCount; i++)
        {
      segments[i] = GenerateSegment();
        }

        return string.Join("-", segments);
    }

  private static string GenerateSegment()
    {
        var chars = new char[SegmentLength];
        var random = Random.Shared;

     for (int i = 0; i < SegmentLength; i++)
 {
    chars[i] = AllowedChars[random.Next(AllowedChars.Length)];
 }

      return new string(chars);
    }
}
