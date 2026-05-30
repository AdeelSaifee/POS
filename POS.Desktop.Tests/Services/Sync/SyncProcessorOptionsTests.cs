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
}
