using System;
using System.Security.Cryptography;
using System.Text;

namespace POS.Desktop.Services.Auth;

/// <summary>
/// Implements secure PIN hashing and verification via PBKDF2.
/// </summary>
public sealed class PinVerifier : IPinVerifier
{
    private const int Iterations = 100_000;
    private const int SaltSize = 16; // 128 bits
    private const int HashSize = 32; // 256 bits
    private static readonly HashAlgorithmName HashAlgorithm = HashAlgorithmName.SHA256;
    private const string AlgorithmName = "PBKDF2_SHA256";

    /// <inheritdoc />
    public bool VerifyPin(string pin, string salt, string hash, string algorithm)
    {
        if (string.IsNullOrEmpty(pin) || string.IsNullOrEmpty(salt) || string.IsNullOrEmpty(hash))
        {
            return false;
        }

        // We support both PBKDF2 and PBKDF2_SHA256 (case-insensitive)
        if (!string.Equals(algorithm, "PBKDF2", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(algorithm, AlgorithmName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        try
        {
            byte[] saltBytes = Convert.FromBase64String(salt);
            byte[] hashBytes = Convert.FromBase64String(hash);

            // Compute hash for comparison
            byte[] computedHashBytes = Rfc2898DeriveBytes.Pbkdf2(
                pin,
                saltBytes,
                Iterations,
                HashAlgorithm,
                hashBytes.Length); // Match the stored hash size

            // Perform constant-time fixed-width byte array comparison to prevent side-channel timing attacks
            return CryptographicOperations.FixedTimeEquals(hashBytes, computedHashBytes);
        }
        catch
        {
            // Fail closed on conversion or decryption failures
            return false;
        }
    }

    /// <inheritdoc />
    public (string Hash, string Salt, string Algorithm) HashPin(string pin)
    {
        if (pin == null) throw new ArgumentNullException(nameof(pin));

        byte[] saltBytes = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] hashBytes = Rfc2898DeriveBytes.Pbkdf2(
            pin,
            saltBytes,
            Iterations,
            HashAlgorithm,
            HashSize);

        return (
            Hash: Convert.ToBase64String(hashBytes),
            Salt: Convert.ToBase64String(saltBytes),
            Algorithm: AlgorithmName
        );
    }
}
