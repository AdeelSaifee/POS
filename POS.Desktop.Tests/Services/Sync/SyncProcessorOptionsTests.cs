using POS.Desktop.Services.Sync;
using Xunit;

namespace POS.Desktop.Tests.Services.Sync;

/// <summary>
/// Unit tests for the <see cref="SyncProcessorOptions"/> configuration validation logic.
/// </summary>
public sealed class SyncProcessorOptionsTests
{
    [Fact]
    public void SyncProcessorOptions_DefaultValues_AreValid()
    {
        // Arrange
        var options = new SyncProcessorOptions();

        // Act
        var isValid = options.Validate(out var errorMessage);

        // Assert
        Assert.True(isValid);
        Assert.Null(errorMessage);
        Assert.Equal(50, options.BatchSize);
        Assert.Equal(10, options.PollIntervalSeconds);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(500)]
    public void SyncProcessorOptions_ValidBatchSize_PassesValidation(int batchSize)
    {
        // Arrange
        var options = new SyncProcessorOptions
        {
            BatchSize = batchSize
        };

        // Act
        var isValid = options.Validate(out var errorMessage);

        // Assert
        Assert.True(isValid);
        Assert.Null(errorMessage);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-500)]
    [InlineData(501)]
    public void SyncProcessorOptions_InvalidBatchSize_FailsValidation(int batchSize)
    {
        // Arrange
        var options = new SyncProcessorOptions
        {
            BatchSize = batchSize
        };

        // Act
        var isValid = options.Validate(out var errorMessage);

        // Assert
        Assert.False(isValid);
        Assert.NotNull(errorMessage);
        Assert.Contains("BatchSize", errorMessage);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(3600)]
    public void SyncProcessorOptions_ValidPollIntervalSeconds_PassesValidation(int pollInterval)
    {
        // Arrange
        var options = new SyncProcessorOptions
        {
            PollIntervalSeconds = pollInterval
        };

        // Act
        var isValid = options.Validate(out var errorMessage);

        // Assert
        Assert.True(isValid);
        Assert.Null(errorMessage);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(3601)]
    public void SyncProcessorOptions_InvalidPollIntervalSeconds_FailsValidation(int pollInterval)
    {
        // Arrange
        var options = new SyncProcessorOptions
        {
            PollIntervalSeconds = pollInterval
        };

        // Act
        var isValid = options.Validate(out var errorMessage);

        // Assert
        Assert.False(isValid);
        Assert.NotNull(errorMessage);
        Assert.Contains("PollIntervalSeconds", errorMessage);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(3601)]
    public void SyncProcessorOptions_InvalidInitialBackoffSeconds_FailsValidation(int value)
    {
        var options = new SyncProcessorOptions { InitialBackoffSeconds = value };
        var isValid = options.Validate(out var errorMessage);
        Assert.False(isValid);
        Assert.NotNull(errorMessage);
        Assert.Contains("InitialBackoffSeconds", errorMessage);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(3601)]
    public void SyncProcessorOptions_InvalidMaxBackoffSeconds_FailsValidation(int value)
    {
        var options = new SyncProcessorOptions { MaxBackoffSeconds = value };
        var isValid = options.Validate(out var errorMessage);
        Assert.False(isValid);
        Assert.NotNull(errorMessage);
        Assert.Contains("MaxBackoffSeconds", errorMessage);
    }

    [Fact]
    public void SyncProcessorOptions_MaxBackoffLessThanInitialBackoff_FailsValidation()
    {
        var options = new SyncProcessorOptions { InitialBackoffSeconds = 10, MaxBackoffSeconds = 5 };
        var isValid = options.Validate(out var errorMessage);
        Assert.False(isValid);
        Assert.NotNull(errorMessage);
        Assert.Contains("MaxBackoffSeconds cannot be less than InitialBackoffSeconds", errorMessage);
    }

    [Theory]
    [InlineData(0.9)]
    [InlineData(10.1)]
    public void SyncProcessorOptions_InvalidBackoffMultiplier_FailsValidation(double value)
    {
        var options = new SyncProcessorOptions { BackoffMultiplier = value };
        var isValid = options.Validate(out var errorMessage);
        Assert.False(isValid);
        Assert.NotNull(errorMessage);
        Assert.Contains("BackoffMultiplier", errorMessage);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(101)]
    public void SyncProcessorOptions_InvalidMaxRetryAttempts_FailsValidation(int value)
    {
        var options = new SyncProcessorOptions { MaxRetryAttempts = value };
        var isValid = options.Validate(out var errorMessage);
        Assert.False(isValid);
        Assert.NotNull(errorMessage);
        Assert.Contains("MaxRetryAttempts", errorMessage);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(100)]
    public void SyncProcessorOptions_ValidMaxRetryAttempts_PassesValidation(int value)
    {
        var options = new SyncProcessorOptions { MaxRetryAttempts = value };
        var isValid = options.Validate(out var errorMessage);
        Assert.True(isValid);
        Assert.Null(errorMessage);
    }
}
