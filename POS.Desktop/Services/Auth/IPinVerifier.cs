namespace POS.Desktop.Services.Auth;

/// <summary>
/// Defines secure verification and generation of salted operator PIN hashes.
/// </summary>
public interface IPinVerifier
{
    /// <summary>
    /// Verifies the given plaintext PIN against a stored salt, hash, and algorithm choice.
    /// Uses per-employee salt and a constant-time comparison helper.
    /// </summary>
    bool VerifyPin(string pin, string salt, string hash, string algorithm);

    /// <summary>
    /// Hashes a new plaintext PIN using standard PBKDF2 with a cryptographically secure random salt.
    /// </summary>
    (string Hash, string Salt, string Algorithm) HashPin(string pin);
}
