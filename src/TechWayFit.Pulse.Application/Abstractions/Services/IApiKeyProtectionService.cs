namespace TechWayFit.Pulse.Application.Abstractions.Services;

/// <summary>
/// Encrypts and decrypts sensitive credential values (e.g. BYOK API keys) using
/// ASP.NET Core Data Protection so they are never stored in plaintext.
/// </summary>
public interface IApiKeyProtectionService
{
    /// <summary>
    /// Encrypts a plaintext API key for safe storage.
    /// </summary>
    string Protect(string plainText);

    /// <summary>
    /// Decrypts a previously protected value. Returns <c>null</c> if the input is
    /// null/empty or if decryption fails (e.g. key was rotated).
    /// </summary>
    string? TryUnprotect(string? cipherText);

    /// <summary>
    /// Returns <c>true</c> if the stored value is non-empty (key has been saved),
    /// without needing to decrypt it.
    /// </summary>
    bool HasKey(string? storedValue);
}
