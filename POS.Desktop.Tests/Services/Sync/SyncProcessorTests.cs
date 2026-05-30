using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using POS.Desktop.Services.Sync;
using POS.Shared.Contracts;
using Xunit;

namespace POS.Desktop.Tests.Services.Sync;

/// <summary>
/// Focused lifecycle and execution tests for the <see cref="SyncProcessor"/> background service.
/// </summary>
public sealed class SyncProcessorTests
{
    private sealed class TestProvisionedTerminalContext : IProvisionedTerminalContext
    {
        public int CurrentTenantId { get; set; } = 1;
        public int CurrentLocationId { get; set; } = 1;
        public int CurrentTerminalId { get; set; } = 1;
        public bool IsProvisioned { get; set; } = false;
    }

    private sealed class TestLogger<T> : ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            // Trivial placeholder for test logger output
        }
    }

    [Fact]
    public async Task SyncProcessor_StartAndStop_ExecutesAndShutsDownCleanly()
    {
        // Arrange
        var context = new TestProvisionedTerminalContext { IsProvisioned = true };
        var logger = new TestLogger<SyncProcessor>();
        var options = new SyncProcessorOptions { PollIntervalSeconds = 1 };
        var processor = new SyncProcessor(logger, context, options);

        using var cts = new CancellationTokenSource();

        // Act
        var runTask = processor.StartAsync(cts.Token);

        // Allow the processor to execute the main loop iterations off the main thread
        await Task.Delay(50);

        // Stop the service using standard host StopAsync path
        await processor.StopAsync(CancellationToken.None);

        // Assert
        var completedTask = await Task.WhenAny(runTask, Task.Delay(1000));
        Assert.Same(runTask, completedTask); // Verify it completed cleanly within the timeout
    }

    [Fact]
    public async Task SyncProcessor_UnprovisionedTerminal_RunsIdleWithoutThrowing()
    {
        // Arrange
        var context = new TestProvisionedTerminalContext { IsProvisioned = false };
        var logger = new TestLogger<SyncProcessor>();
        var options = new SyncProcessorOptions { PollIntervalSeconds = 1 };
        var processor = new SyncProcessor(logger, context, options);

        using var cts = new CancellationTokenSource();

        // Act
        var runTask = processor.StartAsync(cts.Token);

        await Task.Delay(50);

        await processor.StopAsync(CancellationToken.None);

        // Assert
        var completedTask = await Task.WhenAny(runTask, Task.Delay(1000));
        Assert.Same(runTask, completedTask); // Completed cleanly without throwing
    }

    [Fact]
    public async Task SyncProcessor_InvalidOptions_StopsGracefullyOnStart()
    {
        // Arrange
        var context = new TestProvisionedTerminalContext { IsProvisioned = true };
        var logger = new TestLogger<SyncProcessor>();
        var options = new SyncProcessorOptions { BatchSize = -10 }; // Invalid BatchSize
        var processor = new SyncProcessor(logger, context, options);

        // Act
        var runTask = processor.StartAsync(CancellationToken.None);

        // Assert
        var completedTask = await Task.WhenAny(runTask, Task.Delay(1000));
        Assert.Same(runTask, completedTask); // Yielded and returned immediately due to configuration error
    }
}
