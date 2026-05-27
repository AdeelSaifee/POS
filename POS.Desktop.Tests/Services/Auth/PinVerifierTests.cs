using Xunit;
using POS.Desktop.Services.Auth;

namespace POS.Desktop.Tests.Services.Auth;

public class PinVerifierTests
{
    private readonly PinVerifier _verifier = new();

    [Fact]
    public void HashPin_GeneratesNonEmptyHashAndSalt()
    {
        // Act
        var (hash, salt, algorithm) = _verifier.HashPin("1234");

        // Assert
        Assert.False(string.IsNullOrEmpty(hash));
        Assert.False(string.IsNullOrEmpty(salt));
        Assert.Equal("PBKDF2_SHA256", algorithm);
    }

    [Fact]
    public void VerifyPin_Succeeds_WithCorrectPinAndCredentials()
    {
        // Arrange
        string rawPin = "5555";
        var (hash, salt, algorithm) = _verifier.HashPin(rawPin);

        // Act
        bool isValid = _verifier.VerifyPin(rawPin, salt, hash, algorithm);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void VerifyPin_Fails_WithWrongPin()
    {
        // Arrange
        string rawPin = "1111";
        var (hash, salt, algorithm) = _verifier.HashPin(rawPin);

        // Act
        bool isValid = _verifier.VerifyPin("2222", salt, hash, algorithm);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void VerifyPin_Fails_WithUnsupportedAlgorithm()
    {
        // Arrange
        string rawPin = "1234";
        var (hash, salt, _) = _verifier.HashPin(rawPin);

        // Act
        bool isValid = _verifier.VerifyPin(rawPin, salt, hash, "MD5");

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void VerifyPin_Fails_WithEmptyInput()
    {
        // Act & Assert
        Assert.False(_verifier.VerifyPin("", "salt", "hash", "PBKDF2_SHA256"));
        Assert.False(_verifier.VerifyPin("1234", "", "hash", "PBKDF2_SHA256"));
        Assert.False(_verifier.VerifyPin("1234", "salt", "", "PBKDF2_SHA256"));
    }
}
