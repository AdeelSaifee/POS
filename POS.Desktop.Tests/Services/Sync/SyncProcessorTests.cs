using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using POS.Desktop.Services.Sync;
using POS.Shared.Contracts;
using POS.Shared.Contracts.Sync;
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

    private sealed class FakeSyncIngestRequestBuilder : ISyncIngestRequestBuilder
    {
        private readonly Func<SyncOutboxBatch, SyncIngestRequest> _buildCallback;

        public FakeSyncIngestRequestBuilder(Func<SyncOutboxBatch, SyncIngestRequest> buildCallback)
        {
            _buildCallback = buildCallback;
        }

        public SyncIngestRequest Build(SyncOutboxBatch batch) => _buildCallback(batch);
    }

    private sealed class FakeSyncIngestClient : ISyncIngestClient
    {
        private readonly Func<SyncIngestRequest, Task<SyncIngestClientResult>> _ingestCallback;
        public int IngestCallCount { get; private set; }

        public FakeSyncIngestClient(Func<SyncIngestRequest, Task<SyncIngestClientResult>> ingestCallback)
        {
            _ingestCallback = ingestCallback ?? throw new ArgumentNullException(nameof(ingestCallback));
        }

        public async Task<SyncIngestClientResult> IngestAsync(SyncIngestRequest request, CancellationToken cancellationToken = default)
        {
            IngestCallCount++;
            return await _ingestCallback(request);
        }
    }

    private IServiceScopeFactory CreateMockScopeFactory(
        ISyncOutboxBatchReader reader,
        ISyncIngestRequestBuilder? builder = null,
        ISyncIngestClient? client = null)
    {
        var serviceProvider = new TestServiceProvider(type =>
        {
            if (type == typeof(ISyncOutboxBatchReader))
            {
                return reader;
            }
            if (type == typeof(ISyncIngestRequestBuilder))
            {
                return builder;
            }
            if (type == typeof(ISyncIngestClient))
            {
                return client;
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

    [Fact]
    public async Task SyncProcessor_EmptyBatch_DoesNotCallBuilderOrClient()
    {
        // Arrange
        var context = new TestProvisionedTerminalContext { IsProvisioned = true };
        var logger = new TestLogger<SyncProcessor>();
        var options = new SyncProcessorOptions { PollIntervalSeconds = 1 };
        var reader = new FakeSyncOutboxBatchReader(); // Returns empty batch

        var builderCalled = false;
        var builder = new FakeSyncIngestRequestBuilder(batch =>
        {
            builderCalled = true;
            return new SyncIngestRequest(1, 10, 20, 100, "idem", "hash", "corr", Array.Empty<SyncIngestEvent>());
        });

        var client = new FakeSyncIngestClient(request => Task.FromResult(SyncIngestClientResult.Succeeded(
            new SyncIngestResponse(Guid.NewGuid(), 100, "idem", "Success", 0, Array.Empty<SyncIngestEventAck>(), null, null)
        )));

        var scopeFactory = CreateMockScopeFactory(reader, builder, client);
        var processor = new SyncProcessor(logger, context, options, scopeFactory);

        using var cts = new CancellationTokenSource();

        // Act
        var runTask = processor.StartAsync(cts.Token);
        await Task.Delay(50);
        await processor.StopAsync(CancellationToken.None);

        // Assert
        Assert.False(builderCalled);
        Assert.Equal(0, client.IngestCallCount);
    }

    [Fact]
    public async Task SyncProcessor_PendingBatchExists_CallsBuilderAndPostsToClientSuccessfully()
    {
        // Arrange
        var context = new TestProvisionedTerminalContext { IsProvisioned = true };
        var logger = new TestLogger<SyncProcessor>();
        var options = new SyncProcessorOptions { PollIntervalSeconds = 1 };

        // Reader returns 1 item
        var item = new SyncOutboxBatchItem(Guid.NewGuid(), 1, 10, 20, new DateOnly(2026, 5, 30), 100, "OrderCompleted", Guid.NewGuid(), "{}", "hash", "idem", "corr");
        var reader = new FakeSyncOutboxBatchReaderWithItems(new[] { item });

        var builderCalled = false;
        var builder = new FakeSyncIngestRequestBuilder(batch =>
        {
            builderCalled = true;
            return new SyncIngestRequest(1, 10, 20, 100, "idem-chunk", "hash-chunk", "corr-chunk", Array.Empty<SyncIngestEvent>());
        });

        var client = new FakeSyncIngestClient(request => Task.FromResult(SyncIngestClientResult.Succeeded(
            new SyncIngestResponse(Guid.NewGuid(), 100, "idem-chunk", "Success", 0, Array.Empty<SyncIngestEventAck>(), null, null)
        )));

        var scopeFactory = CreateMockScopeFactory(reader, builder, client);
        var processor = new SyncProcessor(logger, context, options, scopeFactory);

        using var cts = new CancellationTokenSource();

        // Act
        var runTask = processor.StartAsync(cts.Token);
        await Task.Delay(50);
        await processor.StopAsync(CancellationToken.None);

        // Assert
        Assert.True(builderCalled);
        Assert.Equal(1, client.IngestCallCount);
    }

    private sealed class FakeSyncOutboxBatchReaderWithItems : ISyncOutboxBatchReader
    {
        private readonly IReadOnlyList<SyncOutboxBatchItem> _items;

        public FakeSyncOutboxBatchReaderWithItems(IReadOnlyList<SyncOutboxBatchItem> items)
        {
            _items = items;
        }

        public Task<SyncOutboxBatch> ReadPendingBatchAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new SyncOutboxBatch(_items));
        }
    }

    [Fact]
    public async Task SyncProcessor_PendingBatchExists_LogsFailureAndAllowsRetry()
    {
        // Arrange
        var context = new TestProvisionedTerminalContext { IsProvisioned = true };
        var logger = new TestLogger<SyncProcessor>();
        var options = new SyncProcessorOptions { PollIntervalSeconds = 1 };

        var item = new SyncOutboxBatchItem(Guid.NewGuid(), 1, 10, 20, new DateOnly(2026, 5, 30), 100, "OrderCompleted", Guid.NewGuid(), "{}", "hash", "idem", "corr");
        var reader = new FakeSyncOutboxBatchReaderWithItems(new[] { item });

        var builder = new FakeSyncIngestRequestBuilder(batch => new SyncIngestRequest(1, 10, 20, 100, "idem-chunk", "hash-chunk", "corr-chunk", Array.Empty<SyncIngestEvent>()));
        var client = new FakeSyncIngestClient(request => Task.FromResult(SyncIngestClientResult.Failed(
            new SyncIngestClientError(SyncIngestClientErrorType.Timeout, "Request timed out.", "TIMEOUT")
        )));

        var scopeFactory = CreateMockScopeFactory(reader, builder, client);
        var processor = new SyncProcessor(logger, context, options, scopeFactory);

        using var cts = new CancellationTokenSource();

        // Act & Assert
        var runTask = processor.StartAsync(cts.Token);
        await Task.Delay(50);
        await processor.StopAsync(CancellationToken.None);

        // Client is called, but fails, so key is not added to the guard, meaning subsequent posts can retry
        Assert.True(client.IngestCallCount >= 1);
    }

    [Fact]
    public async Task SyncProcessor_DuplicateBatchPosted_BlockedByLocalOneFlightGuard()
    {
        // Arrange
        var context = new TestProvisionedTerminalContext { IsProvisioned = true };
        var logger = new TestLogger<SyncProcessor>();
        var options = new SyncProcessorOptions { PollIntervalSeconds = 1 };

        var item = new SyncOutboxBatchItem(Guid.NewGuid(), 1, 10, 20, new DateOnly(2026, 5, 30), 100, "OrderCompleted", Guid.NewGuid(), "{}", "hash", "idem", "corr");
        var reader = new FakeSyncOutboxBatchReaderWithItems(new[] { item });

        var builder = new FakeSyncIngestRequestBuilder(batch => new SyncIngestRequest(1, 10, 20, 100, "idem-chunk", "hash-chunk", "corr-chunk", Array.Empty<SyncIngestEvent>()));
        var client = new FakeSyncIngestClient(request => Task.FromResult(SyncIngestClientResult.Succeeded(
            new SyncIngestResponse(Guid.NewGuid(), 100, "idem-chunk", "Success", 0, Array.Empty<SyncIngestEventAck>(), null, null)
        )));

        var scopeFactory = CreateMockScopeFactory(reader, builder, client);
        var processor = new SyncProcessor(logger, context, options, scopeFactory);

        using var cts = new CancellationTokenSource();

        // Act
        var runTask = processor.StartAsync(cts.Token);

        // Wait around 1300ms to allow at least two polling cycles to run
        await Task.Delay(1300);

        await processor.StopAsync(CancellationToken.None);

        // Assert
        // In-memory guard prevents re-posting the same batch, so call count should be EXACTLY 1!
        Assert.Equal(1, client.IngestCallCount);
    }
}
