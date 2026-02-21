using Microsoft.AspNetCore.DataProtection;
using TechWayFit.Pulse.Application.Abstractions.Services;

namespace TechWayFit.Pulse.Web.Services;

/// <summary>
/// Uses ASP.NET Core Data Protection to encrypt and decrypt facilitator API keys.
/// Keys are protected with a purpose string so they cannot be decrypted by other
/// subsystems even if they share the same Data Protection provider.
/// </summary>
public sealed class ApiKeyProtectionService : IApiKeyProtectionService
{
    private readonly IDataProtector _protector;
    private readonly ILogger<ApiKeyProtectionService> _logger;

    // Versioned purpose string — bump version if key format changes to
    // force re-entry rather than silently failing on old values.
    private const string Purpose = "TechWayFit.Pulse.FacilitatorApiKey.v1";

    public ApiKeyProtectionService(
        IDataProtectionProvider provider,
        ILogger<ApiKeyProtectionService> logger)
    {
        _protector = provider.CreateProtector(Purpose);
        _logger = logger;
    }

    /// <inheritdoc />
    public string Protect(string plainText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plainText);
        return _protector.Protect(plainText);
    }

    /// <inheritdoc />
    public string? TryUnprotect(string? cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
            return null;

        try
        {
            return _protector.Unprotect(cipherText);
        }
        catch (Exception ex)
        {
            // Decryption can fail if Data Protection keys were rotated or the value
            // was stored before encryption was introduced (plaintext migration case).
            // Log at warning level so operators can see if rotation is causing issues.
            _logger.LogWarning(ex, "Failed to decrypt API key — the key may be unencrypted (legacy) or from a rotated key ring");
            return null;
        }
    }

    /// <inheritdoc />
    public bool HasKey(string? storedValue) => !string.IsNullOrEmpty(storedValue);
}
