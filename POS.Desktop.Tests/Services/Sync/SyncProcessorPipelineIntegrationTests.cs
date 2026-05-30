using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using POS.Desktop.Data;
using POS.Desktop.Data.LocalEntities;
using POS.Desktop.Services.Sync;
using POS.Shared.Contracts;
using POS.Shared.Contracts.Sync;
using POS.Shared.Enums;
using Xunit;

namespace POS.Desktop.Tests.Services.Sync;

/// <summary>
/// Integration tests that prove the complete SyncProcessor desktop push pipeline end-to-end:
/// Pending SyncOutbox row → SyncProcessor reads batch → SyncIngestRequestBuilder builds request
/// → ISyncIngestClient posts / receives central-style ack
/// → EfSyncAckApplier marks local row Acked → SyncCursor advances.
///
/// Uses real in-memory SQLite, real EF services, a capturing fake ISyncIngestClient,
/// and a SignalingSyncAckApplier wrapper so assertions wait for the committed SQLite
/// transaction — not an arbitrary sleep.
/// </summary>
public sealed class SyncProcessorPipelineIntegrationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<PosLocalDbContext> _dbOptions;

    public SyncProcessorPipelineIntegrationTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        _dbOptions = new DbContextOptionsBuilder<PosLocalDbContext>()
            .UseSqlite(_connection)
            .Options;
    }

    public void Dispose() => _connection.Dispose();

    // ──────────────────────────────────────────────────────────────────────────
    // Test-local helper types
    // ──────────────────────────────────────────────────────────────────────────

    private sealed class TestProvisionedTerminalContext : IProvisionedTerminalContext
    {
        public int CurrentTenantId { get; init; } = 1;
        public int CurrentLocationId { get; init; } = 10;
        public int CurrentTerminalId { get; init; } = 20;
        public bool IsProvisioned { get; init; } = true;
    }

    /// <summary>
    /// Captures the ingest request and returns a mirrored central-style SyncIngestResponse.
    /// The response mirrors ChunkSequence, ChunkIdempotencyKey, and produces one
    /// SyncIngestEventAck per event — satisfying every validation in EfSyncAckApplier.
    /// </summary>
    private sealed class CapturingSyncIngestClient : ISyncIngestClient
    {
        public int CallCount { get; private set; }
        public SyncIngestRequest? CapturedRequest { get; private set; }

        public Task<SyncIngestClientResult> IngestAsync(
            SyncIngestRequest request,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            CapturedRequest = request;

            var acks = request.Events
                .Select(ev => new SyncIngestEventAck(
                    ev.EventId,
                    ev.IdempotencyKey,
                    ev.TerminalSequence,
                    "Received",
                    null,
                    null))
                .ToList();

            var response = new SyncIngestResponse(
                AckId: Guid.NewGuid(),
                ChunkSequence: request.ChunkSequence,
                ChunkIdempotencyKey: request.ChunkIdempotencyKey,
                Status: "Received",
                EventCount: request.Events.Count,
                Events: acks,
                ErrorCode: null,
                ErrorMessage: null);

            return Task.FromResult(SyncIngestClientResult.Succeeded(response));
        }
    }

    /// <summary>
    /// Wraps the real EfSyncAckApplier and signals a TaskCompletionSource after
    /// ApplySuccessAsync returns. This lets the test synchronize AFTER the SQLite
    /// transaction is committed — not just after the client call returns.
    /// The try/catch pattern ensures the TCS always completes, preventing test hangs
    /// even when the inner applier throws or is cancelled.
    /// </summary>
    private sealed class SignalingSyncAckApplier : ISyncAckApplier
    {
        private readonly ISyncAckApplier _inner;
        private readonly TaskCompletionSource<SyncAckApplyResult> _tcs;

        public SignalingSyncAckApplier(
            ISyncAckApplier inner,
            TaskCompletionSource<SyncAckApplyResult> tcs)
        {
            _inner = inner;
            _tcs = tcs;
        }

        public async Task<SyncAckApplyResult> ApplySuccessAsync(
            SyncOutboxBatch batch,
            SyncIngestRequest request,
            SyncIngestResponse response,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _inner.ApplySuccessAsync(batch, request, response, cancellationToken);
                _tcs.TrySetResult(result);
                return result;
            }
            catch (OperationCanceledException oce)
            {
                _tcs.TrySetCanceled(oce.CancellationToken);
                throw;
            }
            catch (Exception ex)
            {
                _tcs.TrySetException(ex);
                throw;
            }
        }

        public Task<SyncAckApplyResult> ApplyFailureAsync(
            SyncOutboxBatch batch,
            SyncIngestRequest request,
            SyncIngestClientError? error,
            CancellationToken cancellationToken = default)
        {
            return _inner.ApplyFailureAsync(batch, request, error, cancellationToken);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Shared setup helpers
    // ──────────────────────────────────────────────────────────────────────────

    private PosLocalDbContext CreateDbContext(IProvisionedTerminalContext ctx)
    {
        var db = new PosLocalDbContext(_dbOptions, ctx);
        db.Database.EnsureCreated();
        return db;
    }

    private static SyncOutbox MakePendingRow(
        int tenantId = 1,
        int locationId = 10,
        int terminalId = 20,
        long terminalSequence = 100)
    {
        return new SyncOutbox
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            LocationId = locationId,
            TerminalId = terminalId,
            BusinessDate = new DateOnly(2026, 5, 30),
            TerminalSequence = terminalSequence,
            EventType = "OrderCompleted",
            EventId = Guid.NewGuid(),
            PayloadJson = "{\"orderId\":\"test-order-001\"}",
            PayloadHash = "test-payload-hash-001",
            IdempotencyKey = $"idem-seq-{terminalSequence}",
            CorrelationId = $"corr-seq-{terminalSequence}",
            Status = SyncOutboxStatus.Pending,
            IsActive = true,
            AttemptCount = 0,
            LastAttemptOn = null,
            CreatedBy = "test",
            CreatedOn = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Builds a ServiceProvider wired for one processor pipeline cycle.
    /// The fakeClient and ackTcs are created by the caller so they can be
    /// awaited/inspected after the provider is disposed.
    /// PollIntervalSeconds = 60 parks the processor in Task.Delay after the first
    /// cycle; StopAsync cancels that delay immediately — no 60-second wait occurs.
    /// </summary>
    private ServiceProvider BuildPipelineServiceProvider(
        TestProvisionedTerminalContext ctx,
        SyncProcessorOptions options,
        CapturingSyncIngestClient fakeClient,
        TaskCompletionSource<SyncAckApplyResult> ackTcs)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IProvisionedTerminalContext>(ctx);
        services.AddSingleton(options);
        services.AddDbContext<PosLocalDbContext>((_, o) => o.UseSqlite(_connection));
        services.AddScoped<ISyncOutboxBatchReader, EfSyncOutboxBatchReader>();
        services.AddSingleton<ISyncIngestRequestBuilder, SyncIngestRequestBuilder>();
        services.AddSingleton<ISyncIngestClient>(fakeClient);
        services.AddSingleton<ISyncRetryPolicy, SyncRetryPolicy>();

        // Register the concrete EfSyncAckApplier so the wrapper factory can resolve it per scope.
        services.AddScoped<EfSyncAckApplier>();
        services.AddScoped<ISyncQuarantineService, SyncQuarantineService>();
        services.AddScoped<ISyncPaymentReconciliationService, SyncPaymentReconciliationService>();

        // The SignalingSyncAckApplier is registered as the ISyncAckApplier binding.
        // Each scope creates its own instance wrapping the real EfSyncAckApplier; both
        // share the single ackTcs captured in this closure.
        services.AddScoped<ISyncAckApplier>(sp =>
            new SignalingSyncAckApplier(sp.GetRequiredService<EfSyncAckApplier>(), ackTcs));

        return services.BuildServiceProvider();
    }

    private static SyncProcessor BuildProcessor(
        ServiceProvider sp,
        TestProvisionedTerminalContext ctx,
        SyncProcessorOptions options)
    {
        return new SyncProcessor(
            sp.GetRequiredService<ILogger<SyncProcessor>>(),
            ctx,
            options,
            sp.GetRequiredService<ISyncRetryPolicy>(),
            sp.GetRequiredService<IServiceScopeFactory>());
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Tests
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Proves the complete desktop push pipeline:
    /// SyncProcessor reads a Pending outbox row, builds a valid SyncIngestRequest,
    /// posts it to the capturing fake client, receives a central-style SyncIngestResponse,
    /// and persists Acked status + SyncCursor advancement in local SQLite.
    /// </summary>
    [Fact]
    public async Task SyncProcessor_PendingOutboxRow_PostsToCentralStyleClient_AndMarksLocalStateAcked()
    {
        // Arrange ─────────────────────────────────────────────────────────────
        var ctx = new TestProvisionedTerminalContext();
        var options = new SyncProcessorOptions { BatchSize = 50, PollIntervalSeconds = 60 };

        // Seed one Pending outbox row
        using var seedDb = CreateDbContext(ctx);
        var outboxRow = MakePendingRow(terminalSequence: 100);
        seedDb.SyncOutbox.Add(outboxRow);
        await seedDb.SaveChangesAsync();

        var fakeClient = new CapturingSyncIngestClient();
        var ackTcs = new TaskCompletionSource<SyncAckApplyResult>(TaskCreationOptions.RunContinuationsAsynchronously);

        await using var sp = BuildPipelineServiceProvider(ctx, options, fakeClient, ackTcs);
        var processor = BuildProcessor(sp, ctx, options);

        // Act ─────────────────────────────────────────────────────────────────
        await processor.StartAsync(CancellationToken.None);

        // Wait until EfSyncAckApplier.ApplySuccessAsync completes (SQLite transaction committed).
        // The SignalingSyncAckApplier fires ackTcs immediately after the inner applier returns.
        // PollIntervalSeconds = 60 means the processor parks in Task.Delay(60s) after the first
        // cycle; StopAsync cancels that delay immediately — this await does NOT take 60 seconds.
        var ackResult = await ackTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

        await processor.StopAsync(CancellationToken.None);

        // Assert: ack applier reported success ────────────────────────────────
        Assert.True(ackResult.Success);
        Assert.Equal(1, ackResult.AckedRowCount);

        // Assert: capturing fake client received exactly one call
        Assert.Equal(1, fakeClient.CallCount);
        Assert.NotNull(fakeClient.CapturedRequest);

        var req = fakeClient.CapturedRequest!;
        Assert.Equal(1, req.TenantId);
        Assert.Equal(10, req.LocationId);
        Assert.Equal(20, req.TerminalId);
        Assert.Single(req.Events);
        Assert.False(string.IsNullOrWhiteSpace(req.ChunkIdempotencyKey));
        Assert.False(string.IsNullOrWhiteSpace(req.RequestHash));
        Assert.Equal(64, req.RequestHash.Length); // SHA-256 lowercase hex string
        Assert.False(string.IsNullOrWhiteSpace(req.CorrelationId));

        // Assert: SyncOutbox row is Acked in SQLite
        using var assertDb = CreateDbContext(ctx);

        var dbRow = await assertDb.SyncOutbox.FindAsync(outboxRow.Id);
        Assert.NotNull(dbRow);
        Assert.Equal(SyncOutboxStatus.Acked, dbRow.Status);
        Assert.NotNull(dbRow.AckedOn);
        Assert.Equal(req.ChunkSequence, dbRow.ChunkSequence);
        Assert.Null(dbRow.LastErrorCode);
        Assert.Null(dbRow.LastErrorMessage);
        Assert.Equal(0, dbRow.AttemptCount);   // unchanged by ack applier
        Assert.Null(dbRow.LastAttemptOn);      // unchanged by ack applier

        // Assert: SyncCursor exists and is advanced
        var cursor = await assertDb.SyncCursors
            .FirstOrDefaultAsync(x => x.TerminalId == 20 && x.StreamName == "push:outbox");
        Assert.NotNull(cursor);
        Assert.True(cursor.LastPushedChunkSequence >= req.ChunkSequence);
        Assert.True(cursor.LastAckedChunkSequence >= req.ChunkSequence);

        // Assert: a fresh EfSyncOutboxBatchReader sees no more Pending rows after ack
        var reader = new EfSyncOutboxBatchReader(
            assertDb, ctx, options, NullLogger<EfSyncOutboxBatchReader>.Instance);
        var remainingBatch = await reader.ReadPendingBatchAsync();
        Assert.False(remainingBatch.HasItems);
    }

    /// <summary>
    /// Proves that once a row is Acked it disappears from the Pending batch,
    /// so a subsequent processor cycle would find nothing to post.
    /// Uses a deterministic fresh-reader assertion rather than timing-based re-runs.
    /// </summary>
    [Fact]
    public async Task SyncProcessor_AckedRowDisappearsFromPendingBatch_AndDoesNotPostAgain()
    {
        // Arrange ─────────────────────────────────────────────────────────────
        var ctx = new TestProvisionedTerminalContext();
        var options = new SyncProcessorOptions { BatchSize = 50, PollIntervalSeconds = 60 };

        using var seedDb = CreateDbContext(ctx);
        var outboxRow = MakePendingRow(terminalSequence: 200);
        seedDb.SyncOutbox.Add(outboxRow);
        await seedDb.SaveChangesAsync();

        var fakeClient = new CapturingSyncIngestClient();
        var ackTcs = new TaskCompletionSource<SyncAckApplyResult>(TaskCreationOptions.RunContinuationsAsynchronously);

        await using var sp = BuildPipelineServiceProvider(ctx, options, fakeClient, ackTcs);
        var processor = BuildProcessor(sp, ctx, options);

        // Act: run one full processor cycle ───────────────────────────────────
        await processor.StartAsync(CancellationToken.None);
        await ackTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await processor.StopAsync(CancellationToken.None);

        // Assert: row Acked ────────────────────────────────────────────────────
        using var assertDb = CreateDbContext(ctx);
        var dbRow = await assertDb.SyncOutbox.FindAsync(outboxRow.Id);
        Assert.NotNull(dbRow);
        Assert.Equal(SyncOutboxStatus.Acked, dbRow.Status);

        // Assert: fake client was called exactly once (not re-posted)
        Assert.Equal(1, fakeClient.CallCount);

        // Assert: fresh EfSyncOutboxBatchReader returns an empty batch.
        // This is the deterministic proof that a second processor cycle would find
        // nothing to post — the Acked row no longer appears in the Pending query.
        var reader = new EfSyncOutboxBatchReader(
            assertDb, ctx, options, NullLogger<EfSyncOutboxBatchReader>.Instance);
        var batchAfterAck = await reader.ReadPendingBatchAsync();
        Assert.False(batchAfterAck.HasItems);
    }

    /// <summary>
    /// Proves that when a card payment requires reconciliation, completing a push/ack cycle via the SyncProcessor
    /// successfully triggers the reconciliation service, updates the LocalPayment RequiresReconciliation to false,
    /// and transitions the PaymentReconciliationQueue item to ResolvedCaptured.
    /// </summary>
    [Fact]
    public async Task SyncProcessor_ReconciliationClosesLoop_UpdatesQueueAndLocalPayment()
    {
        // Arrange
        var ctx = new TestProvisionedTerminalContext();
        var options = new SyncProcessorOptions { BatchSize = 50, PollIntervalSeconds = 60 };

        var orderId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var tenderMethodId = 2; // Card tender method

        using var seedDb = CreateDbContext(ctx);

        var payment = new LocalPayment
        {
            Id = paymentId,
            TenantId = ctx.CurrentTenantId,
            OrderId = orderId,
            LocationId = ctx.CurrentLocationId,
            TerminalId = ctx.CurrentTerminalId,
            TenderMethodId = tenderMethodId,
            BusinessDate = new DateOnly(2026, 5, 30),
            TerminalSequence = 101,
            PaymentType = PaymentType.Sale,
            Status = PaymentStatus.Paid,
            Amount = 150m,
            RequiresReconciliation = true,
            ProcessedOn = DateTimeOffset.UtcNow,
            IsActive = true,
            CreatedBy = "test",
            CreatedOn = DateTimeOffset.UtcNow
        };

        var queueRow = new PaymentReconciliationQueue
        {
            Id = Guid.NewGuid(),
            TenantId = ctx.CurrentTenantId,
            LocationId = ctx.CurrentLocationId,
            TerminalId = ctx.CurrentTerminalId,
            OrderId = orderId,
            PaymentId = paymentId,
            TenderMethodId = tenderMethodId,
            ExternalPaymentReference = "TXN-CARD-99",
            Status = PaymentReconciliationStatus.Pending,
            AttemptCount = 0,
            IdempotencyKey = $"reconciliation:payment:{paymentId}",
            IsActive = true,
            CreatedBy = "test",
            CreatedOn = DateTimeOffset.UtcNow
        };

        var outboxRow = new SyncOutbox
        {
            Id = Guid.NewGuid(),
            TenantId = ctx.CurrentTenantId,
            LocationId = ctx.CurrentLocationId,
            TerminalId = ctx.CurrentTerminalId,
            BusinessDate = new DateOnly(2026, 5, 30),
            TerminalSequence = 101,
            EventType = "OrderCompleted",
            EventId = orderId,
            PayloadJson = "{\"orderId\":\"" + orderId + "\"}",
            PayloadHash = "test-payload-hash-002",
            IdempotencyKey = $"order-completed:{orderId}",
            CorrelationId = "test-corr-id",
            Status = SyncOutboxStatus.Pending,
            IsActive = true,
            AttemptCount = 0,
            CreatedBy = "test",
            CreatedOn = DateTimeOffset.UtcNow
        };

        seedDb.LocalPayments.Add(payment);
        seedDb.PaymentReconciliationQueue.Add(queueRow);
        seedDb.SyncOutbox.Add(outboxRow);
        await seedDb.SaveChangesAsync();

        var fakeClient = new CapturingSyncIngestClient();
        var ackTcs = new TaskCompletionSource<SyncAckApplyResult>(TaskCreationOptions.RunContinuationsAsynchronously);

        await using var sp = BuildPipelineServiceProvider(ctx, options, fakeClient, ackTcs);
        var processor = BuildProcessor(sp, ctx, options);

        // Act
        await processor.StartAsync(CancellationToken.None);
        var ackResult = await ackTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await processor.StopAsync(CancellationToken.None);

        // Assert
        Assert.True(ackResult.Success);

        // Verify Db rows
        using var assertDb = CreateDbContext(ctx);
        var updatedPayment = await assertDb.LocalPayments.FindAsync(paymentId);
        Assert.NotNull(updatedPayment);
        Assert.False(updatedPayment.RequiresReconciliation);
        Assert.NotNull(updatedPayment.ReconciledOn);

        var updatedQueueRow = await assertDb.PaymentReconciliationQueue.FirstOrDefaultAsync(q => q.PaymentId == paymentId);
        Assert.NotNull(updatedQueueRow);
        Assert.Equal(PaymentReconciliationStatus.ResolvedCaptured, updatedQueueRow.Status);
        Assert.Equal("SYNC_ACK", updatedQueueRow.LastResultCode);
    }
}
