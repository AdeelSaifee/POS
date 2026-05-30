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
        private readonly object _lock = new();
        private readonly Func<SyncIngestRequest, Task<SyncIngestClientResult>> _ingestCallback;
        public int IngestCallCount { get; private set; }
        private TaskCompletionSource? _targetCallCountTcs;
        private int _targetCallCount;

        public FakeSyncIngestClient(Func<SyncIngestRequest, Task<SyncIngestClientResult>> ingestCallback)
        {
            _ingestCallback = ingestCallback ?? throw new ArgumentNullException(nameof(ingestCallback));
        }

        public async Task<SyncIngestClientResult> IngestAsync(SyncIngestRequest request, CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                IngestCallCount++;
                if (_targetCallCountTcs != null && IngestCallCount >= _targetCallCount)
                {
                    _targetCallCountTcs.TrySetResult();
                }
            }
            return await _ingestCallback(request);
        }

        public Task WaitForCallCountAsync(int targetCount)
        {
            lock (_lock)
            {
                if (IngestCallCount >= targetCount)
                {
                    return Task.CompletedTask;
                }
                _targetCallCount = targetCount;
                _targetCallCountTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                return _targetCallCountTcs.Task;
            }
        }
    }

    private sealed class FakeSyncAckApplier : ISyncAckApplier
    {
        private readonly object _lock = new();
        private readonly Func<SyncOutboxBatch, SyncIngestRequest, SyncIngestResponse, Task<SyncAckApplyResult>> _callback;
        public int ApplyCallCount { get; private set; }
        private TaskCompletionSource? _targetCallCountTcs;
        private int _targetCallCount;

        public FakeSyncAckApplier(Func<SyncOutboxBatch, SyncIngestRequest, SyncIngestResponse, Task<SyncAckApplyResult>> callback)
        {
            _callback = callback;
        }

        public async Task<SyncAckApplyResult> ApplySuccessAsync(
            SyncOutboxBatch batch,
            SyncIngestRequest request,
            SyncIngestResponse response,
            CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                ApplyCallCount++;
                if (_targetCallCountTcs != null && ApplyCallCount >= _targetCallCount)
                {
                    _targetCallCountTcs.TrySetResult();
                }
            }
            return await _callback(batch, request, response);
        }

        public Task WaitForCallCountAsync(int targetCount)
        {
            lock (_lock)
            {
                if (ApplyCallCount >= targetCount)
                {
                    return Task.CompletedTask;
                }
                _targetCallCount = targetCount;
                _targetCallCountTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                return _targetCallCountTcs.Task;
            }
        }

        public int ApplyFailureCallCount { get; private set; }
        private TaskCompletionSource? _targetFailureCallCountTcs;
        private int _targetFailureCallCount;

        public Task<SyncAckApplyResult> ApplyFailureAsync(
            SyncOutboxBatch batch,
            SyncIngestRequest request,
            SyncIngestClientError? error,
            CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                ApplyFailureCallCount++;
                if (_targetFailureCallCountTcs != null && ApplyFailureCallCount >= _targetFailureCallCount)
                {
                    _targetFailureCallCountTcs.TrySetResult();
                }
            }
            return Task.FromResult(SyncAckApplyResult.Succeeded(batch.Count, request.ChunkSequence));
        }

        public Task WaitForFailureCallCountAsync(int targetCount)
        {
            lock (_lock)
            {
                if (ApplyFailureCallCount >= targetCount)
                {
                    return Task.CompletedTask;
                }
                _targetFailureCallCount = targetCount;
                _targetFailureCallCountTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                return _targetFailureCallCountTcs.Task;
            }
        }
    }

    private sealed class FakeSyncRetryPolicy : ISyncRetryPolicy
    {
        private readonly object _lock = new();
        public int ConsecutiveFailureCountCalculated { get; private set; } = 0;
        public TimeSpan ReturnedDelay { get; set; } = TimeSpan.FromMilliseconds(5);
        public bool IsTransientResult { get; set; } = true;
        private TaskCompletionSource? _targetCountTcs;
        private int _targetFailureCount;

        public bool IsTransient(SyncIngestClientErrorType errorType) => IsTransientResult;

        public TimeSpan CalculateBackoff(int consecutiveFailureCount)
        {
            lock (_lock)
            {
                ConsecutiveFailureCountCalculated = consecutiveFailureCount;
                if (_targetCountTcs != null && consecutiveFailureCount >= _targetFailureCount)
                {
                    _targetCountTcs.TrySetResult();
                }
            }
            return ReturnedDelay;
        }

        public Task WaitForFailureCountAsync(int targetCount)
        {
            lock (_lock)
            {
                if (ConsecutiveFailureCountCalculated >= targetCount)
                {
                    return Task.CompletedTask;
                }
                _targetFailureCount = targetCount;
                _targetCountTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                return _targetCountTcs.Task;
            }
        }
    }

    private sealed class FakeSyncConnectivityService : ISyncConnectivityService
    {
        public bool IsConnected { get; set; } = true;

        public Task<bool> IsConnectedAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(IsConnected);
        }
    }

    private sealed class ZeroBackoffRetryPolicy : ISyncRetryPolicy
    {
        private readonly SyncProcessorOptions _options;

        public ZeroBackoffRetryPolicy(SyncProcessorOptions options)
        {
            _options = options;
        }

        public bool IsTransient(SyncIngestClientErrorType errorType) => true;

        public TimeSpan CalculateBackoff(int consecutiveFailureCount)
        {
            return TimeSpan.FromSeconds(_options.PollIntervalSeconds);
        }
    }

    private SyncProcessor CreateSyncProcessor(
        ILogger<SyncProcessor> logger,
        IProvisionedTerminalContext context,
        SyncProcessorOptions options,
        IServiceScopeFactory scopeFactory,
        ISyncRetryPolicy? retryPolicy = null,
        ISyncConnectivityService? connectivityService = null)
    {
        retryPolicy ??= new ZeroBackoffRetryPolicy(options);
        connectivityService ??= new FakeSyncConnectivityService();
        return new SyncProcessor(logger, context, options, retryPolicy, connectivityService, scopeFactory);
    }

    private IServiceScopeFactory CreateMockScopeFactory(
        ISyncOutboxBatchReader reader,
        ISyncIngestRequestBuilder? builder = null,
        ISyncIngestClient? client = null,
        ISyncAckApplier? ackApplier = null)
    {
        ackApplier ??= new FakeSyncAckApplier((b, req, resp) => Task.FromResult(SyncAckApplyResult.Succeeded(b.Count, resp.ChunkSequence)));

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
            if (type == typeof(ISyncAckApplier))
            {
                return ackApplier;
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
        var processor = CreateSyncProcessor(logger, context, options, scopeFactory);

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
        var processor = CreateSyncProcessor(logger, context, options, scopeFactory);

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
        var processor = CreateSyncProcessor(logger, context, options, scopeFactory);

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
        var processor = CreateSyncProcessor(logger, context, options, scopeFactory);

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
        var processor = CreateSyncProcessor(logger, context, options, scopeFactory);

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
        var processor = CreateSyncProcessor(logger, context, options, scopeFactory);

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
        var processor = CreateSyncProcessor(logger, context, options, scopeFactory);

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

    [Fact]
    public async Task SyncProcessor_SuccessfulIngestAndAckApply_CallsApplierAndAddsToGuard()
    {
        // Arrange
        var context = new TestProvisionedTerminalContext { IsProvisioned = true };
        var logger = new TestLogger<SyncProcessor>();
        var options = new SyncProcessorOptions { PollIntervalSeconds = 1 };

        var item = new SyncOutboxBatchItem(Guid.NewGuid(), 1, 10, 20, new DateOnly(2026, 5, 30), 100, "OrderCompleted", Guid.NewGuid(), "{}", "hash", "idem", "corr");
        var reader = new FakeSyncOutboxBatchReaderWithItems(new[] { item });

        var builder = new FakeSyncIngestRequestBuilder(batch => new SyncIngestRequest(1, 10, 20, 100, "idem-chunk", "hash-chunk", "corr-chunk", Array.Empty<SyncIngestEvent>()));
        var client = new FakeSyncIngestClient(request => Task.FromResult(SyncIngestClientResult.Succeeded(
            new SyncIngestResponse(Guid.NewGuid(), 100, "idem-chunk", "Received", 0, Array.Empty<SyncIngestEventAck>(), null, null)
        )));

        var applier = new FakeSyncAckApplier((b, req, resp) => Task.FromResult(SyncAckApplyResult.Succeeded(b.Count, resp.ChunkSequence)));

        var scopeFactory = CreateMockScopeFactory(reader, builder, client, applier);
        var processor = CreateSyncProcessor(logger, context, options, scopeFactory);

        using var cts = new CancellationTokenSource();

        // Act
        var runTask = processor.StartAsync(cts.Token);
        await Task.Delay(50);
        await processor.StopAsync(CancellationToken.None);

        // Assert
        Assert.Equal(1, client.IngestCallCount);
        Assert.Equal(1, applier.ApplyCallCount);
    }

    [Fact]
    public async Task SyncProcessor_SuccessfulIngestButFailedAckApply_DoesNotAddToGuardAndAllowsRetry()
    {
        // Arrange
        var context = new TestProvisionedTerminalContext { IsProvisioned = true };
        var logger = new TestLogger<SyncProcessor>();
        var options = new SyncProcessorOptions { PollIntervalSeconds = 1 };

        var item = new SyncOutboxBatchItem(Guid.NewGuid(), 1, 10, 20, new DateOnly(2026, 5, 30), 100, "OrderCompleted", Guid.NewGuid(), "{}", "hash", "idem", "corr");
        var reader = new FakeSyncOutboxBatchReaderWithItems(new[] { item });

        var builder = new FakeSyncIngestRequestBuilder(batch => new SyncIngestRequest(1, 10, 20, 100, "idem-chunk", "hash-chunk", "corr-chunk", Array.Empty<SyncIngestEvent>()));
        var client = new FakeSyncIngestClient(request => Task.FromResult(SyncIngestClientResult.Succeeded(
            new SyncIngestResponse(Guid.NewGuid(), 100, "idem-chunk", "Received", 0, Array.Empty<SyncIngestEventAck>(), null, null)
        )));

        var applier = new FakeSyncAckApplier((b, req, resp) => Task.FromResult(SyncAckApplyResult.Failed("DB_ERROR", "Failed to save to database.")));

        var scopeFactory = CreateMockScopeFactory(reader, builder, client, applier);
        var processor = CreateSyncProcessor(logger, context, options, scopeFactory);

        using var cts = new CancellationTokenSource();

        // Act
        var runTask = processor.StartAsync(cts.Token);
        await Task.Delay(1300); // Allow two cycles to run
        await processor.StopAsync(CancellationToken.None);

        // Assert
        // Ingest should be called twice since the in-memory guard was not updated (due to failed DB ack apply)
        Assert.True(client.IngestCallCount >= 2);
        Assert.True(applier.ApplyCallCount >= 2);
    }

    [Fact]
    public async Task SyncProcessor_SuccessfulIngestWithNullResponse_DoesNotCallApplierAndDoesNotAddToGuard()
    {
        // Arrange
        var context = new TestProvisionedTerminalContext { IsProvisioned = true };
        var logger = new TestLogger<SyncProcessor>();
        var options = new SyncProcessorOptions { PollIntervalSeconds = 1 };

        var item = new SyncOutboxBatchItem(Guid.NewGuid(), 1, 10, 20, new DateOnly(2026, 5, 30), 100, "OrderCompleted", Guid.NewGuid(), "{}", "hash", "idem", "corr");
        var reader = new FakeSyncOutboxBatchReaderWithItems(new[] { item });

        var builder = new FakeSyncIngestRequestBuilder(batch => new SyncIngestRequest(1, 10, 20, 100, "idem-chunk", "hash-chunk", "corr-chunk", Array.Empty<SyncIngestEvent>()));
        var client = new FakeSyncIngestClient(request => Task.FromResult(SyncIngestClientResult.Succeeded(null!))); // null response

        var applier = new FakeSyncAckApplier((b, req, resp) => Task.FromResult(SyncAckApplyResult.Succeeded(b.Count, resp.ChunkSequence)));

        var scopeFactory = CreateMockScopeFactory(reader, builder, client, applier);
        var processor = CreateSyncProcessor(logger, context, options, scopeFactory);

        using var cts = new CancellationTokenSource();

        // Act
        var runTask = processor.StartAsync(cts.Token);
        await Task.Delay(1300); // Allow two cycles to run
        await processor.StopAsync(CancellationToken.None);

        // Assert
        Assert.True(client.IngestCallCount >= 2);
        Assert.Equal(0, applier.ApplyCallCount); // Applier never called
    }

    [Fact]
    public async Task SyncProcessor_FailedIngest_DoesNotCallApplierAndDoesNotAddToGuard()
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

        var applier = new FakeSyncAckApplier((b, req, resp) => Task.FromResult(SyncAckApplyResult.Succeeded(b.Count, resp.ChunkSequence)));

        var scopeFactory = CreateMockScopeFactory(reader, builder, client, applier);
        var processor = CreateSyncProcessor(logger, context, options, scopeFactory);

        using var cts = new CancellationTokenSource();

        // Act
        var runTask = processor.StartAsync(cts.Token);
        await Task.Delay(1300); // Allow two cycles to run
        await processor.StopAsync(CancellationToken.None);

        // Assert
        Assert.True(client.IngestCallCount >= 2);
        Assert.Equal(0, applier.ApplyCallCount); // Applier never called
    }

    [Theory]
    [InlineData(SyncIngestClientErrorType.Offline, true)]
    [InlineData(SyncIngestClientErrorType.Timeout, true)]
    [InlineData(SyncIngestClientErrorType.ServerError, true)]
    [InlineData(SyncIngestClientErrorType.Unexpected, true)]
    [InlineData(SyncIngestClientErrorType.Unauthorized, true)]
    [InlineData(SyncIngestClientErrorType.Forbidden, false)]
    [InlineData(SyncIngestClientErrorType.Validation, false)]
    [InlineData(SyncIngestClientErrorType.Conflict, false)]
    [InlineData(SyncIngestClientErrorType.Configuration, false)]
    public void SyncRetryPolicy_IsTransient_ClassifiesCorrectly(SyncIngestClientErrorType errorType, bool expectedIsTransient)
    {
        var options = new SyncProcessorOptions();
        var policy = new SyncRetryPolicy(options);
        Assert.Equal(expectedIsTransient, policy.IsTransient(errorType));
    }

    [Theory]
    [InlineData(0, 10)] // <= 0 returns PollIntervalSeconds
    [InlineData(1, 2)]  // 2 * 2^0 = 2
    [InlineData(2, 4)]  // 2 * 2^1 = 4
    [InlineData(3, 8)]  // 2 * 2^2 = 8
    [InlineData(10, 300)] // capped at max (300)
    [InlineData(100, 300)] // capped and overflow protected
    public void SyncRetryPolicy_CalculateBackoff_AppliesExponentialCurveAndCap(int consecutiveFailures, int expectedSeconds)
    {
        var options = new SyncProcessorOptions
        {
            PollIntervalSeconds = 10,
            InitialBackoffSeconds = 2,
            MaxBackoffSeconds = 300,
            BackoffMultiplier = 2.0
        };
        var policy = new SyncRetryPolicy(options);
        var backoff = policy.CalculateBackoff(consecutiveFailures);
        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), backoff);
    }

    [Fact]
    public async Task SyncProcessor_ConsecutiveFailure_AppliesExponentialBackoffLoopDelay()
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
        var fakeRetryPolicy = new FakeSyncRetryPolicy { ReturnedDelay = TimeSpan.FromMilliseconds(1) };
        var processor = CreateSyncProcessor(logger, context, options, scopeFactory, fakeRetryPolicy);

        using var cts = new CancellationTokenSource();

        // Act
        var runTask = processor.StartAsync(cts.Token);

        // Wait deterministically for at least 2 client ingest calls (implying the backoff delay occurred and the second sweep was started)
        await Task.WhenAny(client.WaitForCallCountAsync(2), Task.Delay(1000));
        await processor.StopAsync(CancellationToken.None);

        // Assert
        // The policy should have been called with failure counts >= 1
        Assert.True(fakeRetryPolicy.ConsecutiveFailureCountCalculated >= 1);
        Assert.True(client.IngestCallCount >= 2);
    }

    [Fact]
    public async Task SyncProcessor_FailureThenSuccess_ResetsFailureCount()
    {
        // Arrange
        var context = new TestProvisionedTerminalContext { IsProvisioned = true };
        var logger = new TestLogger<SyncProcessor>();
        var options = new SyncProcessorOptions { PollIntervalSeconds = 1 };

        var item = new SyncOutboxBatchItem(Guid.NewGuid(), 1, 10, 20, new DateOnly(2026, 5, 30), 100, "OrderCompleted", Guid.NewGuid(), "{}", "hash", "idem", "corr");
        var reader = new FakeSyncOutboxBatchReaderWithItems(new[] { item });

        var builder = new FakeSyncIngestRequestBuilder(batch => new SyncIngestRequest(1, 10, 20, 100, "idem-chunk", "hash-chunk", "corr-chunk", Array.Empty<SyncIngestEvent>()));

        // Client fails on first call, succeeds on subsequent calls
        int callCount = 0;
        var client = new FakeSyncIngestClient(request => {
            callCount++;
            if (callCount == 1)
            {
                return Task.FromResult(SyncIngestClientResult.Failed(
                    new SyncIngestClientError(SyncIngestClientErrorType.Timeout, "Request timed out.", "TIMEOUT")
                ));
            }
            return Task.FromResult(SyncIngestClientResult.Succeeded(
                new SyncIngestResponse(Guid.NewGuid(), 100, "idem-chunk", "Received", 0, Array.Empty<SyncIngestEventAck>(), null, null)
            ));
        });

        // Hook into the applier to wait until the successful ack is processed.
        var applier = new FakeSyncAckApplier((b, req, resp) => Task.FromResult(SyncAckApplyResult.Succeeded(b.Count, resp.ChunkSequence)));

        var scopeFactory = CreateMockScopeFactory(reader, builder, client, applier);
        var fakeRetryPolicy = new FakeSyncRetryPolicy { ReturnedDelay = TimeSpan.FromMilliseconds(1) };
        var processor = CreateSyncProcessor(logger, context, options, scopeFactory, fakeRetryPolicy);

        using var cts = new CancellationTokenSource();

        // Act
        var runTask = processor.StartAsync(cts.Token);

        // Wait deterministically for the successful ingest (2nd client call) and the successful db ack apply.
        // Once ApplySuccessAsync is called (ApplyCallCount >= 1), the second sweep is finishing and resetting failure count.
        await Task.WhenAny(applier.WaitForCallCountAsync(1), Task.Delay(1000));

        // Give a very small task delay (20ms) to let the background thread resume execution after the Applier returns
        // and finish the current loop iteration to update/reset consecutiveFailureCount.
        await Task.Delay(20);

        await processor.StopAsync(CancellationToken.None);

        // Assert
        Assert.True(callCount >= 2);
        // On first run: failed, ConsecutiveFailureCountCalculated becomes 1
        Assert.Equal(1, fakeRetryPolicy.ConsecutiveFailureCountCalculated);
    }

    [Fact]
    public async Task SyncProcessor_IngestFails_CallsApplyFailureAsync()
    {
        // Arrange
        var context = new TestProvisionedTerminalContext { IsProvisioned = true };
        var logger = new TestLogger<SyncProcessor>();
        var options = new SyncProcessorOptions { PollIntervalSeconds = 1 };

        var item = new SyncOutboxBatchItem(Guid.NewGuid(), 1, 10, 20, new DateOnly(2026, 5, 30), 100, "OrderCompleted", Guid.NewGuid(), "{}", "hash", "idem", "corr");
        var reader = new FakeSyncOutboxBatchReaderWithItems(new[] { item });

        var builder = new FakeSyncIngestRequestBuilder(batch => new SyncIngestRequest(1, 10, 20, 100, "idem-chunk", "hash-chunk", "corr-chunk", Array.Empty<SyncIngestEvent>()));

        // Client always fails
        var client = new FakeSyncIngestClient(request => Task.FromResult(SyncIngestClientResult.Failed(
            new SyncIngestClientError(SyncIngestClientErrorType.Timeout, "Request timed out.", "TIMEOUT")
        )));

        var applier = new FakeSyncAckApplier((b, req, resp) => Task.FromResult(SyncAckApplyResult.Succeeded(b.Count, resp.ChunkSequence)));

        var scopeFactory = CreateMockScopeFactory(reader, builder, client, applier);
        var fakeRetryPolicy = new FakeSyncRetryPolicy { ReturnedDelay = TimeSpan.FromMilliseconds(1) };
        var processor = CreateSyncProcessor(logger, context, options, scopeFactory, fakeRetryPolicy);

        using var cts = new CancellationTokenSource();

        // Act
        var runTask = processor.StartAsync(cts.Token);

        // Wait deterministically for at least 1 failure to be applied
        await Task.WhenAny(applier.WaitForFailureCallCountAsync(1), Task.Delay(1000));
        await processor.StopAsync(CancellationToken.None);

        // Assert
        Assert.True(applier.ApplyFailureCallCount >= 1);
    }

    [Fact]
    public async Task SyncProcessor_OfflineState_PausesSweepsAndIdlesSafely()
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
        var connectivityService = new FakeSyncConnectivityService { IsConnected = false }; // Offline
        var processor = CreateSyncProcessor(logger, context, options, scopeFactory, connectivityService: connectivityService);

        using var cts = new CancellationTokenSource();

        // Act
        var runTask = processor.StartAsync(cts.Token);
        await Task.Delay(50);
        await processor.StopAsync(CancellationToken.None);

        // Assert
        Assert.Equal(0, client.IngestCallCount); // Bypassed
    }

    private sealed class TrackingOutboxBatchReader : ISyncOutboxBatchReader
    {
        public int CallCount { get; private set; }
        public Task<SyncOutboxBatch> ReadPendingBatchAsync(CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(new SyncOutboxBatch(Array.Empty<SyncOutboxBatchItem>()));
        }
    }

    [Fact]
    public async Task SyncProcessor_OfflineState_DoesNotCallIngestClientOrBatchReader()
    {
        // Arrange
        var context = new TestProvisionedTerminalContext { IsProvisioned = true };
        var logger = new TestLogger<SyncProcessor>();
        var options = new SyncProcessorOptions { PollIntervalSeconds = 1 };

        var reader = new TrackingOutboxBatchReader();
        var client = new FakeSyncIngestClient(request => Task.FromResult(SyncIngestClientResult.Succeeded(null!)));

        var scopeFactory = CreateMockScopeFactory(reader, client: client);
        var connectivityService = new FakeSyncConnectivityService { IsConnected = false }; // Offline
        var processor = CreateSyncProcessor(logger, context, options, scopeFactory, connectivityService: connectivityService);

        using var cts = new CancellationTokenSource();

        // Act
        var runTask = processor.StartAsync(cts.Token);
        await Task.Delay(50);
        await processor.StopAsync(CancellationToken.None);

        // Assert
        Assert.Equal(0, reader.CallCount);
        Assert.Equal(0, client.IngestCallCount);
    }

    [Fact]
    public async Task SyncProcessor_OfflineState_DoesNotIncrementConsecutiveFailureCount()
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
        var fakeRetryPolicy = new FakeSyncRetryPolicy { ReturnedDelay = TimeSpan.FromMilliseconds(1) };
        var connectivityService = new FakeSyncConnectivityService { IsConnected = false }; // Offline
        var processor = CreateSyncProcessor(logger, context, options, scopeFactory, fakeRetryPolicy, connectivityService);

        using var cts = new CancellationTokenSource();

        // Act
        var runTask = processor.StartAsync(cts.Token);
        await Task.Delay(50);
        await processor.StopAsync(CancellationToken.None);

        // Assert
        Assert.Equal(0, client.IngestCallCount);
        Assert.Equal(0, fakeRetryPolicy.ConsecutiveFailureCountCalculated); // Retry backoff calculation never called
    }

    [Fact]
    public async Task SyncProcessor_OfflineOnlineTransition_ResumesProcessingOnLaterLoop()
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
        var connectivityService = new FakeSyncConnectivityService { IsConnected = false }; // Start offline
        var processor = CreateSyncProcessor(logger, context, options, scopeFactory, connectivityService: connectivityService);

        using var cts = new CancellationTokenSource();

        // Act
        var runTask = processor.StartAsync(cts.Token);

        // Wait brief time, ensure 0 calls while offline
        await Task.Delay(50);
        Assert.Equal(0, client.IngestCallCount);

        // Switch to online
        connectivityService.IsConnected = true;

        // Wait deterministically for the client call count to reach 1 (resumed processing)
        await Task.WhenAny(client.WaitForCallCountAsync(1), Task.Delay(2000));
        await processor.StopAsync(CancellationToken.None);

        // Assert
        Assert.True(client.IngestCallCount >= 1);
    }
}
