using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
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
        public int CurrentLocationId { get; set; } = 10;
        public int CurrentTerminalId { get; set; } = 20;
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

    private sealed class FakeSyncOutboxBatchReader : ISyncOutboxBatchReader
    {
        public Task<SyncOutboxBatch> ReadPendingBatchAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new SyncOutboxBatch(Array.Empty<SyncOutboxBatchItem>()));
        }
    }

    private sealed class TestServiceScope : IServiceScope
    {
        public IServiceProvider ServiceProvider { get; }

        public TestServiceScope(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }

        public void Dispose() { }
    }

    private sealed class TestServiceScopeFactory : IServiceScopeFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public TestServiceScopeFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IServiceScope CreateScope()
        {
            return new TestServiceScope(_serviceProvider);
        }
    }

    private sealed class TestServiceProvider : IServiceProvider
    {
        private readonly Func<Type, object?> _resolver;

        public TestServiceProvider(Func<Type, object?> resolver)
        {
            _resolver = resolver;
        }

        public object? GetService(Type serviceType)
        {
            return _resolver(serviceType);
        }
    }

    private IServiceScopeFactory CreateMockScopeFactory(ISyncOutboxBatchReader reader)
    {
        var serviceProvider = new TestServiceProvider(type =>
        {
            if (type == typeof(ISyncOutboxBatchReader))
            {
                return reader;
            }
            return null;
        });
        return new TestServiceScopeFactory(serviceProvider);
    }

    [Fact]
    public async Task SyncProcessor_StartAndStop_ExecutesAndShutsDownCleanly()
    {
        // Arrange
        var context = new TestProvisionedTerminalContext { IsProvisioned = true };
        var logger = new TestLogger<SyncProcessor>();
        var options = new SyncProcessorOptions { PollIntervalSeconds = 1 };
        var reader = new FakeSyncOutboxBatchReader();
        var scopeFactory = CreateMockScopeFactory(reader);
        var processor = new SyncProcessor(logger, context, options, scopeFactory);

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
        var reader = new FakeSyncOutboxBatchReader();
        var scopeFactory = CreateMockScopeFactory(reader);
        var processor = new SyncProcessor(logger, context, options, scopeFactory);

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
        var reader = new FakeSyncOutboxBatchReader();
        var scopeFactory = CreateMockScopeFactory(reader);
        var processor = new SyncProcessor(logger, context, options, scopeFactory);

        // Act
        var runTask = processor.StartAsync(CancellationToken.None);

        // Assert
        var completedTask = await Task.WhenAny(runTask, Task.Delay(1000));
        Assert.Same(runTask, completedTask); // Yielded and returned immediately due to configuration error
    }
}
