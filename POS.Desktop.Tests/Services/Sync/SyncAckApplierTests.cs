using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
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
/// Database integration and validation tests for <see cref="EfSyncAckApplier"/>.
/// </summary>
public sealed class SyncAckApplierTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<PosLocalDbContext> _options;

    public SyncAckApplierTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<PosLocalDbContext>()
            .UseSqlite(_connection)
            .Options;
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    private PosLocalDbContext CreateDbContext(IProvisionedTerminalContext context)
    {
        var db = new PosLocalDbContext(_options, context);
        db.Database.EnsureCreated();
        return db;
    }

    private sealed class TestProvisionedTerminalContext : IProvisionedTerminalContext
    {
        public int CurrentTenantId { get; set; } = 1;
        public int CurrentLocationId { get; set; } = 10;
        public int CurrentTerminalId { get; set; } = 20;
        public bool IsProvisioned { get; set; } = true;
    }

    private SyncOutbox CreateTestOutbox(
        Guid? id = null,
        int tenantId = 1,
        int locationId = 10,
        int terminalId = 20,
        long terminalSequence = 100,
        SyncOutboxStatus status = SyncOutboxStatus.Pending,
        string eventType = "OrderCompleted",
        Guid? eventId = null,
        string idempotencyKey = "idem-123",
        string payloadHash = "hash-123",
        string payloadJson = "{}",
        string correlationId = "corr-123",
        bool isActive = true)
    {
        return new SyncOutbox
        {
            Id = id ?? Guid.NewGuid(),
            TenantId = tenantId,
            LocationId = locationId,
            TerminalId = terminalId,
            BusinessDate = new DateOnly(2026, 5, 30),
            TerminalSequence = terminalSequence,
            EventType = eventType,
            EventId = eventId ?? Guid.NewGuid(),
            PayloadJson = payloadJson,
            PayloadHash = payloadHash,
            IdempotencyKey = idempotencyKey,
            CorrelationId = correlationId,
            Status = status,
            IsActive = isActive,
            CreatedBy = "Operator",
            CreatedOn = DateTimeOffset.UtcNow
        };
    }

    private SyncOutboxBatchItem ToBatchItem(SyncOutbox row)
    {
        return new SyncOutboxBatchItem(
            row.Id,
            row.TenantId,
            row.LocationId,
            row.TerminalId,
            row.BusinessDate,
            row.TerminalSequence,
            row.EventType,
            row.EventId,
            row.PayloadJson,
            row.PayloadHash,
            row.IdempotencyKey,
            row.CorrelationId
        );
    }

    private SyncIngestEvent ToRequestEvent(SyncOutboxBatchItem item, long chunkSequence)
    {
        return new SyncIngestEvent(
            item.BusinessDate,
            item.TerminalSequence,
            item.EventType,
            item.EventId,
            item.PayloadJson,
            item.PayloadHash,
            item.IdempotencyKey,
            item.CorrelationId,
            chunkSequence
        );
    }

    private SyncIngestEventAck ToResponseAck(SyncIngestEvent ev, string status = "Received")
    {
        return new SyncIngestEventAck(
            ev.EventId,
            ev.IdempotencyKey,
            ev.TerminalSequence,
            status,
            null,
            null
        );
    }

    [Fact]
    public async Task ApplySuccess_ThrowsOrFails_WhenBatchRequestResponseNull()
    {
        // Arrange
        var context = new TestProvisionedTerminalContext();
        using var db = CreateDbContext(context);
        var applier = new EfSyncAckApplier(db, context, NullLogger<EfSyncAckApplier>.Instance);

        var batch = new SyncOutboxBatch(Array.Empty<SyncOutboxBatchItem>());
        var request = new SyncIngestRequest(1, 10, 20, 100, "idem", "hash", "corr", Array.Empty<SyncIngestEvent>());
        var response = new SyncIngestResponse(Guid.NewGuid(), 100, "idem", "Received", 0, Array.Empty<SyncIngestEventAck>(), null, null);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => applier.ApplySuccessAsync(null!, request, response));
        await Assert.ThrowsAsync<ArgumentNullException>(() => applier.ApplySuccessAsync(batch, null!, response));
        await Assert.ThrowsAsync<ArgumentNullException>(() => applier.ApplySuccessAsync(batch, request, null!));
    }

    [Fact]
    public async Task ApplySuccess_Fails_WhenTerminalUnprovisioned()
    {
        // Arrange
        var context = new TestProvisionedTerminalContext { IsProvisioned = false };
        using var db = CreateDbContext(context);
        var applier = new EfSyncAckApplier(db, context, NullLogger<EfSyncAckApplier>.Instance);

        var batch = new SyncOutboxBatch(Array.Empty<SyncOutboxBatchItem>());
        var request = new SyncIngestRequest(1, 10, 20, 100, "idem", "hash", "corr", Array.Empty<SyncIngestEvent>());
        var response = new SyncIngestResponse(Guid.NewGuid(), 100, "idem", "Received", 0, Array.Empty<SyncIngestEventAck>(), null, null);

        // Act
        var result = await applier.ApplySuccessAsync(batch, request, response);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("UNCONFIGURED_TERMINAL", result.ErrorCode);
    }

    [Fact]
    public async Task ApplySuccess_Fails_WhenIdentityMismatchesProvisionedTerminal()
    {
        // Arrange
        var context = new TestProvisionedTerminalContext();
        using var db = CreateDbContext(context);
        var applier = new EfSyncAckApplier(db, context, NullLogger<EfSyncAckApplier>.Instance);

        var batch = new SyncOutboxBatch(Array.Empty<SyncOutboxBatchItem>());
        var request = new SyncIngestRequest(2, 10, 20, 100, "idem", "hash", "corr", Array.Empty<SyncIngestEvent>()); // tenant 2 instead of 1
        var response = new SyncIngestResponse(Guid.NewGuid(), 100, "idem", "Received", 0, Array.Empty<SyncIngestEventAck>(), null, null);

        // Act
        var result = await applier.ApplySuccessAsync(batch, request, response);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("IDENTITY_MISMATCH", result.ErrorCode);
    }

    [Fact]
    public async Task ApplySuccess_Fails_WhenResponseChunkSequenceMismatch()
    {
        // Arrange
        var context = new TestProvisionedTerminalContext();
        using var db = CreateDbContext(context);
        var applier = new EfSyncAckApplier(db, context, NullLogger<EfSyncAckApplier>.Instance);

        var batch = new SyncOutboxBatch(Array.Empty<SyncOutboxBatchItem>());
        var request = new SyncIngestRequest(1, 10, 20, 100, "idem", "hash", "corr", Array.Empty<SyncIngestEvent>());
        var response = new SyncIngestResponse(Guid.NewGuid(), 101, "idem", "Received", 0, Array.Empty<SyncIngestEventAck>(), null, null); // seq 101 instead of 100

        // Act
        var result = await applier.ApplySuccessAsync(batch, request, response);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("SEQUENCE_MISMATCH", result.ErrorCode);
    }

    [Fact]
    public async Task ApplySuccess_Fails_WhenResponseChunkIdempotencyKeyMismatch()
    {
        // Arrange
        var context = new TestProvisionedTerminalContext();
        using var db = CreateDbContext(context);
        var applier = new EfSyncAckApplier(db, context, NullLogger<EfSyncAckApplier>.Instance);

        var batch = new SyncOutboxBatch(Array.Empty<SyncOutboxBatchItem>());
        var request = new SyncIngestRequest(1, 10, 20, 100, "idem-A", "hash", "corr", Array.Empty<SyncIngestEvent>());
        var response = new SyncIngestResponse(Guid.NewGuid(), 100, "idem-B", "Received", 0, Array.Empty<SyncIngestEventAck>(), null, null);

        // Act
        var result = await applier.ApplySuccessAsync(batch, request, response);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("IDEMPOTENCY_KEY_MISMATCH", result.ErrorCode);
    }

    [Fact]
    public async Task ApplySuccess_Fails_WhenResponseStatusNotReceived()
    {
        // Arrange
        var context = new TestProvisionedTerminalContext();
        using var db = CreateDbContext(context);
        var applier = new EfSyncAckApplier(db, context, NullLogger<EfSyncAckApplier>.Instance);

        var batch = new SyncOutboxBatch(Array.Empty<SyncOutboxBatchItem>());
        var request = new SyncIngestRequest(1, 10, 20, 100, "idem", "hash", "corr", Array.Empty<SyncIngestEvent>());
        var response = new SyncIngestResponse(Guid.NewGuid(), 100, "idem", "Failed", 0, Array.Empty<SyncIngestEventAck>(), null, null); // Failed status

        // Act
        var result = await applier.ApplySuccessAsync(batch, request, response);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("INVALID_RESPONSE_STATUS", result.ErrorCode);
    }

    [Fact]
    public async Task ApplySuccess_Fails_WhenEventCountMismatch()
    {
        // Arrange
        var context = new TestProvisionedTerminalContext();
        using var db = CreateDbContext(context);
        var applier = new EfSyncAckApplier(db, context, NullLogger<EfSyncAckApplier>.Instance);

        var row = CreateTestOutbox();
        var item = ToBatchItem(row);
        var ev = ToRequestEvent(item, 100);

        var batch = new SyncOutboxBatch(new[] { item });
        var request = new SyncIngestRequest(1, 10, 20, 100, "idem", "hash", "corr", new[] { ev });
        var response = new SyncIngestResponse(Guid.NewGuid(), 100, "idem", "Received", 0, Array.Empty<SyncIngestEventAck>(), null, null); // count 0 instead of 1

        // Act
        var result = await applier.ApplySuccessAsync(batch, request, response);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("EVENT_COUNT_MISMATCH", result.ErrorCode);
    }

    [Fact]
    public async Task ApplySuccess_Fails_WhenAckMissingForRequestEvent()
    {
        // Arrange
        var context = new TestProvisionedTerminalContext();
        using var db = CreateDbContext(context);
        var applier = new EfSyncAckApplier(db, context, NullLogger<EfSyncAckApplier>.Instance);

        var row = CreateTestOutbox();
        var item = ToBatchItem(row);
        var ev = ToRequestEvent(item, 100);

        var batch = new SyncOutboxBatch(new[] { item });
        var request = new SyncIngestRequest(1, 10, 20, 100, "idem", "hash", "corr", new[] { ev });

        // Ack EventId mismatch
        var ack = new SyncIngestEventAck(Guid.NewGuid(), ev.IdempotencyKey, ev.TerminalSequence, "Received", null, null);
        var response = new SyncIngestResponse(Guid.NewGuid(), 100, "idem", "Received", 1, new[] { ack }, null, null);

        // Act
        var result = await applier.ApplySuccessAsync(batch, request, response);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("EVENT_ACK_MISMATCH", result.ErrorCode);
    }

    [Fact]
    public async Task ApplySuccess_Fails_WhenAckStatusNotReceived()
    {
        // Arrange
        var context = new TestProvisionedTerminalContext();
        using var db = CreateDbContext(context);
        var applier = new EfSyncAckApplier(db, context, NullLogger<EfSyncAckApplier>.Instance);

        var row = CreateTestOutbox();
        var item = ToBatchItem(row);
        var ev = ToRequestEvent(item, 100);

        var batch = new SyncOutboxBatch(new[] { item });
        var request = new SyncIngestRequest(1, 10, 20, 100, "idem", "hash", "corr", new[] { ev });

        var ack = ToResponseAck(ev, "Failed"); // Non-received event ack status
        var response = new SyncIngestResponse(Guid.NewGuid(), 100, "idem", "Received", 1, new[] { ack }, null, null);

        // Act
        var result = await applier.ApplySuccessAsync(batch, request, response);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("EVENT_ACK_FAILED", result.ErrorCode);
    }

    [Fact]
    public async Task ApplySuccess_Fails_WhenMatchingOutboxRowMissing()
    {
        // Arrange
        var context = new TestProvisionedTerminalContext();
        using var db = CreateDbContext(context);
        var applier = new EfSyncAckApplier(db, context, NullLogger<EfSyncAckApplier>.Instance);

        var row = CreateTestOutbox();
        // Row is NOT saved in the database!

        var item = ToBatchItem(row);
        var ev = ToRequestEvent(item, 100);
        var batch = new SyncOutboxBatch(new[] { item });
        var request = new SyncIngestRequest(1, 10, 20, 100, "idem", "hash", "corr", new[] { ev });
        var ack = ToResponseAck(ev);
        var response = new SyncIngestResponse(Guid.NewGuid(), 100, "idem", "Received", 1, new[] { ack }, null, null);

        // Act
        var result = await applier.ApplySuccessAsync(batch, request, response);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("OUTBOX_ROWS_MISSING", result.ErrorCode);
    }

    [Fact]
    public async Task ApplySuccess_Fails_WhenOutboxRowAlreadyAckedOrNotPending()
    {
        // Arrange
        var context = new TestProvisionedTerminalContext();
        using var db = CreateDbContext(context);
        var applier = new EfSyncAckApplier(db, context, NullLogger<EfSyncAckApplier>.Instance);

        var row = CreateTestOutbox(status: SyncOutboxStatus.Acked); // Already Acked
        db.SyncOutbox.Add(row);
        await db.SaveChangesAsync();

        var item = ToBatchItem(row);
        var ev = ToRequestEvent(item, 100);
        var batch = new SyncOutboxBatch(new[] { item });
        var request = new SyncIngestRequest(1, 10, 20, 100, "idem", "hash", "corr", new[] { ev });
        var ack = ToResponseAck(ev);
        var response = new SyncIngestResponse(Guid.NewGuid(), 100, "idem", "Received", 1, new[] { ack }, null, null);

        // Act
        var result = await applier.ApplySuccessAsync(batch, request, response);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("OUTBOX_ROWS_MISSING", result.ErrorCode);
    }

    [Fact]
    public async Task ApplySuccess_MarksAllRowsAcked_AndSetsAckMetadata()
    {
        // Arrange
        var context = new TestProvisionedTerminalContext();
        using var db = CreateDbContext(context);
        var applier = new EfSyncAckApplier(db, context, NullLogger<EfSyncAckApplier>.Instance);

        var row = CreateTestOutbox();
        db.SyncOutbox.Add(row);
        await db.SaveChangesAsync();

        var item = ToBatchItem(row);
        var ev = ToRequestEvent(item, 100);
        var batch = new SyncOutboxBatch(new[] { item });
        var request = new SyncIngestRequest(1, 10, 20, 100, "idem", "hash", "corr", new[] { ev });
        var ack = ToResponseAck(ev);
        var response = new SyncIngestResponse(Guid.NewGuid(), 100, "idem", "Received", 1, new[] { ack }, null, null);

        // Act
        var result = await applier.ApplySuccessAsync(batch, request, response);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(1, result.AckedRowCount);
        Assert.Equal(100, result.LastAckedChunkSequence);

        var dbRow = await db.SyncOutbox.FindAsync(row.Id);
        Assert.NotNull(dbRow);
        Assert.Equal(SyncOutboxStatus.Acked, dbRow.Status);
        Assert.NotNull(dbRow.AckedOn);
        Assert.Equal(100, dbRow.ChunkSequence);
        Assert.Null(dbRow.LastErrorCode);
        Assert.Null(dbRow.LastErrorMessage);
        Assert.Equal("sync-processor", dbRow.UpdatedBy);
        Assert.NotNull(dbRow.UpdatedOn);
    }

    [Fact]
    public async Task ApplySuccess_CreatesCursor_WhenMissing()
    {
        // Arrange
        var context = new TestProvisionedTerminalContext();
        using var db = CreateDbContext(context);
        var applier = new EfSyncAckApplier(db, context, NullLogger<EfSyncAckApplier>.Instance);

        var row = CreateTestOutbox();
        db.SyncOutbox.Add(row);
        await db.SaveChangesAsync();

        var item = ToBatchItem(row);
        var ev = ToRequestEvent(item, 100);
        var batch = new SyncOutboxBatch(new[] { item });
        var request = new SyncIngestRequest(1, 10, 20, 100, "idem", "hash", "corr", new[] { ev });
        var ack = ToResponseAck(ev);
        var response = new SyncIngestResponse(Guid.NewGuid(), 100, "idem", "Received", 1, new[] { ack }, null, null);

        // Act
        var result = await applier.ApplySuccessAsync(batch, request, response);

        // Assert
        Assert.True(result.Success);

        var cursor = await db.SyncCursors.FirstOrDefaultAsync(x => x.TerminalId == 20 && x.StreamName == "push:outbox");
        Assert.NotNull(cursor);
        Assert.Equal(1, cursor.TenantId);
        Assert.Equal(10, cursor.LocationId);
        Assert.Equal(20, cursor.TerminalId);
        Assert.Equal("push:outbox", cursor.StreamName);
        Assert.Equal(100, cursor.LastPushedChunkSequence);
        Assert.Equal(100, cursor.LastAckedChunkSequence);
        Assert.Equal(SyncCursorStatus.Active, cursor.Status);
    }

    [Fact]
    public async Task ApplySuccess_UpdatesCursorMonotonically_WhenExistingLowerSequence()
    {
        // Arrange
        var context = new TestProvisionedTerminalContext();
        using var db = CreateDbContext(context);
        var applier = new EfSyncAckApplier(db, context, NullLogger<EfSyncAckApplier>.Instance);

        var row = CreateTestOutbox();
        db.SyncOutbox.Add(row);

        // Pre-create lower sequence cursor
        var existingCursor = new SyncCursor
        {
            Id = Guid.NewGuid(),
            TenantId = 1,
            LocationId = 10,
            TerminalId = 20,
            StreamName = "push:outbox",
            LastPushedChunkSequence = 50,
            LastAckedChunkSequence = 50,
            Status = SyncCursorStatus.Active,
            IsActive = true,
            CreatedBy = "Operator",
            CreatedOn = DateTimeOffset.UtcNow
        };
        db.SyncCursors.Add(existingCursor);
        await db.SaveChangesAsync();

        var item = ToBatchItem(row);
        var ev = ToRequestEvent(item, 100);
        var batch = new SyncOutboxBatch(new[] { item });
        var request = new SyncIngestRequest(1, 10, 20, 100, "idem", "hash", "corr", new[] { ev });
        var ack = ToResponseAck(ev);
        var response = new SyncIngestResponse(Guid.NewGuid(), 100, "idem", "Received", 1, new[] { ack }, null, null);

        // Act
        var result = await applier.ApplySuccessAsync(batch, request, response);

        // Assert
        Assert.True(result.Success);

        var cursor = await db.SyncCursors.FirstOrDefaultAsync(x => x.TerminalId == 20 && x.StreamName == "push:outbox");
        Assert.NotNull(cursor);
        Assert.Equal(100, cursor.LastPushedChunkSequence);
        Assert.Equal(100, cursor.LastAckedChunkSequence);
    }

    [Fact]
    public async Task ApplySuccess_DoesNotRegressCursor_WhenExistingHigherSequence()
    {
        // Arrange
        var context = new TestProvisionedTerminalContext();
        using var db = CreateDbContext(context);
        var applier = new EfSyncAckApplier(db, context, NullLogger<EfSyncAckApplier>.Instance);

        var row = CreateTestOutbox();
        db.SyncOutbox.Add(row);

        // Pre-create HIGHER sequence cursor
        var existingCursor = new SyncCursor
        {
            Id = Guid.NewGuid(),
            TenantId = 1,
            LocationId = 10,
            TerminalId = 20,
            StreamName = "push:outbox",
            LastPushedChunkSequence = 200,
            LastAckedChunkSequence = 200,
            Status = SyncCursorStatus.Active,
            IsActive = true,
            CreatedBy = "Operator",
            CreatedOn = DateTimeOffset.UtcNow
        };
        db.SyncCursors.Add(existingCursor);
        await db.SaveChangesAsync();

        var item = ToBatchItem(row);
        var ev = ToRequestEvent(item, 100);
        var batch = new SyncOutboxBatch(new[] { item });
        var request = new SyncIngestRequest(1, 10, 20, 100, "idem", "hash", "corr", new[] { ev });
        var ack = ToResponseAck(ev);
        var response = new SyncIngestResponse(Guid.NewGuid(), 100, "idem", "Received", 1, new[] { ack }, null, null);

        // Act
        var result = await applier.ApplySuccessAsync(batch, request, response);

        // Assert
        Assert.True(result.Success);

        var cursor = await db.SyncCursors.FirstOrDefaultAsync(x => x.TerminalId == 20 && x.StreamName == "push:outbox");
        Assert.NotNull(cursor);
        Assert.Equal(200, cursor.LastPushedChunkSequence); // Preserved higher sequence 200, did not regress!
        Assert.Equal(200, cursor.LastAckedChunkSequence);
    }

    [Fact]
    public async Task ApplySuccess_RollsBackAll_WhenAnyRowInvalid()
    {
        // Arrange
        var context = new TestProvisionedTerminalContext();
        using var db = CreateDbContext(context);
        var applier = new EfSyncAckApplier(db, context, NullLogger<EfSyncAckApplier>.Instance);

        var row1 = CreateTestOutbox(terminalSequence: 100);
        var row2 = CreateTestOutbox(terminalSequence: 101);
        db.SyncOutbox.Add(row1);
        // row2 is NOT saved in DB!
        await db.SaveChangesAsync();

        var item1 = ToBatchItem(row1);
        var item2 = ToBatchItem(row2);
        var ev1 = ToRequestEvent(item1, 100);
        var ev2 = ToRequestEvent(item2, 100);

        var batch = new SyncOutboxBatch(new[] { item1, item2 });
        var request = new SyncIngestRequest(1, 10, 20, 100, "idem", "hash", "corr", new[] { ev1, ev2 });
        var ack1 = ToResponseAck(ev1);
        var ack2 = ToResponseAck(ev2);
        var response = new SyncIngestResponse(Guid.NewGuid(), 100, "idem", "Received", 2, new[] { ack1, ack2 }, null, null);

        // Act
        var result = await applier.ApplySuccessAsync(batch, request, response);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("OUTBOX_ROWS_MISSING", result.ErrorCode);

        // Verify row1 remains Pending in DB (rolled back completely)
        var dbRow = await db.SyncOutbox.FindAsync(row1.Id);
        Assert.NotNull(dbRow);
        Assert.Equal(SyncOutboxStatus.Pending, dbRow.Status);

        // Verify no cursor was created
        var cursor = await db.SyncCursors.FirstOrDefaultAsync(x => x.TerminalId == 20 && x.StreamName == "push:outbox");
        Assert.Null(cursor);
    }

    [Fact]
    public async Task ApplySuccess_DoesNotUpdateAttemptCountOrLastAttemptOn()
    {
        // Arrange
        var context = new TestProvisionedTerminalContext();
        using var db = CreateDbContext(context);
        var applier = new EfSyncAckApplier(db, context, NullLogger<EfSyncAckApplier>.Instance);

        var row = CreateTestOutbox();
        row.AttemptCount = 3;
        row.LastAttemptOn = new DateTimeOffset(2026, 5, 30, 6, 0, 0, TimeSpan.Zero);
        db.SyncOutbox.Add(row);
        await db.SaveChangesAsync();

        var item = ToBatchItem(row);
        var ev = ToRequestEvent(item, 100);
        var batch = new SyncOutboxBatch(new[] { item });
        var request = new SyncIngestRequest(1, 10, 20, 100, "idem", "hash", "corr", new[] { ev });
        var ack = ToResponseAck(ev);
        var response = new SyncIngestResponse(Guid.NewGuid(), 100, "idem", "Received", 1, new[] { ack }, null, null);

        // Act
        var result = await applier.ApplySuccessAsync(batch, request, response);

        // Assert
        Assert.True(result.Success);

        var dbRow = await db.SyncOutbox.FindAsync(row.Id);
        Assert.NotNull(dbRow);
        Assert.Equal(3, dbRow.AttemptCount); // Preserved
        Assert.Equal(new DateTimeOffset(2026, 5, 30, 6, 0, 0, TimeSpan.Zero), dbRow.LastAttemptOn); // Preserved
    }

    [Fact]
    public async Task ApplySuccess_FromFailedToAcked_SuccessfullyTransitions()
    {
        // Arrange
        var context = new TestProvisionedTerminalContext();
        using var db = CreateDbContext(context);
        var applier = new EfSyncAckApplier(db, context, NullLogger<EfSyncAckApplier>.Instance);

        var row = CreateTestOutbox();
        row.Status = SyncOutboxStatus.Failed; // Start as Failed
        row.AttemptCount = 2;
        db.SyncOutbox.Add(row);
        await db.SaveChangesAsync();

        var item = ToBatchItem(row);
        var ev = ToRequestEvent(item, 101);
        var batch = new SyncOutboxBatch(new[] { item });
        var request = new SyncIngestRequest(1, 10, 20, 101, "idem", "hash", "corr", new[] { ev });
        var ack = ToResponseAck(ev);
        var response = new SyncIngestResponse(Guid.NewGuid(), 101, "idem", "Received", 1, new[] { ack }, null, null);

        // Act
        var result = await applier.ApplySuccessAsync(batch, request, response);

        // Assert
        Assert.True(result.Success);

        var dbRow = await db.SyncOutbox.FindAsync(row.Id);
        Assert.NotNull(dbRow);
        Assert.Equal(SyncOutboxStatus.Acked, dbRow.Status);
    }

    [Fact]
    public async Task ApplyFailure_FirstFailure_IncrementsCountAndSetsFailedStatus()
    {
        // Arrange
        var context = new TestProvisionedTerminalContext();
        using var db = CreateDbContext(context);
        var options = new SyncProcessorOptions { MaxRetryAttempts = 3 };
        var applier = new EfSyncAckApplier(db, context, options, NullLogger<EfSyncAckApplier>.Instance);

        var row = CreateTestOutbox();
        row.Status = SyncOutboxStatus.Pending;
        row.AttemptCount = 0;
        db.SyncOutbox.Add(row);
        await db.SaveChangesAsync();

        var item = ToBatchItem(row);
        var ev = ToRequestEvent(item, 200);
        var batch = new SyncOutboxBatch(new[] { item });
        var request = new SyncIngestRequest(1, 10, 20, 200, "idem", "hash", "corr", new[] { ev });
        var error = new SyncIngestClientError(SyncIngestClientErrorType.Timeout, "Request timed out", "TIMEOUT_ERROR");

        // Act
        var result = await applier.ApplyFailureAsync(batch, request, error);

        // Assert
        Assert.True(result.Success);

        var dbRow = await db.SyncOutbox.FindAsync(row.Id);
        Assert.NotNull(dbRow);
        Assert.Equal(SyncOutboxStatus.Failed, dbRow.Status);
        Assert.Equal(1, dbRow.AttemptCount);
        Assert.NotNull(dbRow.LastAttemptOn);
        Assert.Equal("TIMEOUT_ERROR", dbRow.LastErrorCode);
        Assert.Equal("Request timed out", dbRow.LastErrorMessage);
    }

    [Fact]
    public async Task ApplyFailure_ReachesMaxAttempts_TransitionsToDeadLetterAndCreatesRecoveryJournalEntry()
    {
        // Arrange
        var context = new TestProvisionedTerminalContext();
        using var db = CreateDbContext(context);
        var options = new SyncProcessorOptions { MaxRetryAttempts = 3 };
        var applier = new EfSyncAckApplier(db, context, options, NullLogger<EfSyncAckApplier>.Instance);

        var row = CreateTestOutbox();
        row.Status = SyncOutboxStatus.Failed;
        row.AttemptCount = 2; // Next failure will make it 3 (equal to MaxRetryAttempts)
        db.SyncOutbox.Add(row);
        await db.SaveChangesAsync();

        var item = ToBatchItem(row);
        var ev = ToRequestEvent(item, 300);
        var batch = new SyncOutboxBatch(new[] { item });
        var request = new SyncIngestRequest(1, 10, 20, 300, "idem", "hash", "corr", new[] { ev });
        var error = new SyncIngestClientError(SyncIngestClientErrorType.Conflict, "Conflict occurred", "CONFLICT_ERROR");

        // Act
        var result = await applier.ApplyFailureAsync(batch, request, error);

        // Assert
        Assert.True(result.Success);

        var dbRow = await db.SyncOutbox.FindAsync(row.Id);
        Assert.NotNull(dbRow);
        Assert.Equal(SyncOutboxStatus.DeadLetter, dbRow.Status);
        Assert.Equal(3, dbRow.AttemptCount);

        // Assert recovery journal entry
        var journalEntry = await db.LocalRecoveryJournal.FirstOrDefaultAsync(x => x.IdempotencyKey == $"quarantine:syncoutbox:{row.Id}");
        Assert.NotNull(journalEntry);
        Assert.Equal(RecoveryType.SyncInFlight, journalEntry.RecoveryType);
        Assert.Equal(RecoveryJournalStatus.Open, journalEntry.Status);
        Assert.Equal(RequiredRecoveryAction.RetrySync, journalEntry.RequiredAction);
        Assert.Equal(row.CorrelationId, journalEntry.CorrelationId);

        // Validate safe payload does not leak database business PayloadJson
        Assert.DoesNotContain(row.PayloadJson, journalEntry.StatePayloadJson);

        var payloadDoc = System.Text.Json.JsonDocument.Parse(journalEntry.StatePayloadJson);
        Assert.Equal(row.Id.ToString(), payloadDoc.RootElement.GetProperty("OutboxId").GetString());
        Assert.Equal("DeadLetter", payloadDoc.RootElement.GetProperty("Status").GetString());
        Assert.Equal("CONFLICT_ERROR", payloadDoc.RootElement.GetProperty("LastErrorCode").GetString());
        Assert.Equal(3, payloadDoc.RootElement.GetProperty("AttemptCount").GetInt32());
    }

    [Fact]
    public async Task ApplyFailure_PreventsDuplicateRecoveryJournalEntries()
    {
        // Arrange
        var context = new TestProvisionedTerminalContext();
        using var db = CreateDbContext(context);
        var options = new SyncProcessorOptions { MaxRetryAttempts = 3 };
        var applier = new EfSyncAckApplier(db, context, options, NullLogger<EfSyncAckApplier>.Instance);

        var row = CreateTestOutbox();
        row.Status = SyncOutboxStatus.Failed;
        row.AttemptCount = 2; // Next failure will make it 3
        db.SyncOutbox.Add(row);

        // Manually seed an existing journal entry with the same idempotency key
        var existingJournal = new LocalRecoveryJournal
        {
            Id = Guid.NewGuid(),
            TenantId = context.CurrentTenantId,
            LocationId = context.CurrentLocationId,
            TerminalId = context.CurrentTerminalId,
            RecoveryType = RecoveryType.SyncInFlight,
            Status = RecoveryJournalStatus.Open,
            RequiredAction = RequiredRecoveryAction.RetrySync,
            StatePayloadJson = "{}",
            IdempotencyKey = $"quarantine:syncoutbox:{row.Id}",
            CorrelationId = row.CorrelationId,
            IsActive = true,
            CreatedBy = "test-seeding",
            CreatedOn = DateTimeOffset.UtcNow
        };
        db.LocalRecoveryJournal.Add(existingJournal);
        await db.SaveChangesAsync();

        var item = ToBatchItem(row);
        var ev = ToRequestEvent(item, 400);
        var batch = new SyncOutboxBatch(new[] { item });
        var request = new SyncIngestRequest(1, 10, 20, 400, "idem", "hash", "corr", new[] { ev });
        var error = new SyncIngestClientError(SyncIngestClientErrorType.Conflict, "Conflict occurred", "CONFLICT_ERROR");

        // Act
        var result = await applier.ApplyFailureAsync(batch, request, error);

        // Assert
        Assert.True(result.Success);

        // Count should still be exactly 1 in the DB (the seed wasn't duplicated/overwritten)
        var count = await db.LocalRecoveryJournal.CountAsync(x => x.IdempotencyKey == $"quarantine:syncoutbox:{row.Id}");
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task SyncQuarantineService_GetQuarantinedItems_ReturnsSafeMetadata()
    {
        // Arrange
        var context = new TestProvisionedTerminalContext();
        using var db = CreateDbContext(context);

        var pendingRow = CreateTestOutbox(idempotencyKey: "idem-pending");
        pendingRow.Status = SyncOutboxStatus.Pending;

        var deadLetterRow = CreateTestOutbox(idempotencyKey: "idem-deadletter");
        deadLetterRow.Status = SyncOutboxStatus.DeadLetter;
        deadLetterRow.AttemptCount = 5;
        deadLetterRow.LastAttemptOn = DateTimeOffset.UtcNow;
        deadLetterRow.LastErrorCode = "TEST_CODE";
        deadLetterRow.LastErrorMessage = "TEST_MESSAGE";

        db.SyncOutbox.AddRange(pendingRow, deadLetterRow);
        await db.SaveChangesAsync();

        var service = new SyncQuarantineService(db, context);

        // Act
        var items = await service.GetQuarantinedItemsAsync();

        // Assert
        Assert.Single(items);
        var item = items[0];
        Assert.Equal(deadLetterRow.Id, item.OutboxId);
        Assert.Equal(deadLetterRow.EventType, item.EventType);
        Assert.Equal(deadLetterRow.EventId, item.EventId);
        Assert.Equal("TEST_CODE", item.LastErrorCode);
        Assert.Equal("TEST_MESSAGE", item.LastErrorMessage);
        Assert.Equal(5, item.AttemptCount);
    }

    [Fact]
    public async Task SyncQuarantineService_GetQuarantinedItems_UnprovisionedTerminal_ReturnsEmpty()
    {
        // Arrange
        var context = new TestProvisionedTerminalContext { IsProvisioned = false };
        using var db = CreateDbContext(context);

        var deadLetterRow = CreateTestOutbox(idempotencyKey: "idem-deadletter");
        deadLetterRow.Status = SyncOutboxStatus.DeadLetter;

        db.SyncOutbox.Add(deadLetterRow);
        await db.SaveChangesAsync();

        var service = new SyncQuarantineService(db, context);

        // Act
        var items = await service.GetQuarantinedItemsAsync();

        // Assert
        Assert.Empty(items);
    }

    [Fact]
    public async Task SyncQuarantineService_GetQuarantinedItems_DifferentLocationOrTerminal_IsExcluded()
    {
        // Arrange
        var context = new TestProvisionedTerminalContext { CurrentLocationId = 10, CurrentTerminalId = 20 };
        using var db = CreateDbContext(context);

        // Different location
        var diffLocationRow = CreateTestOutbox(idempotencyKey: "idem-diff-loc", locationId: 11, terminalId: 20);
        diffLocationRow.Status = SyncOutboxStatus.DeadLetter;

        // Different terminal
        var diffTerminalRow = CreateTestOutbox(idempotencyKey: "idem-diff-term", locationId: 10, terminalId: 21);
        diffTerminalRow.Status = SyncOutboxStatus.DeadLetter;

        // Current location/terminal (active deadletter)
        var currentMatchRow = CreateTestOutbox(idempotencyKey: "idem-match", locationId: 10, terminalId: 20);
        currentMatchRow.Status = SyncOutboxStatus.DeadLetter;

        db.SyncOutbox.AddRange(diffLocationRow, diffTerminalRow, currentMatchRow);
        await db.SaveChangesAsync();

        var service = new SyncQuarantineService(db, context);

        // Act
        var items = await service.GetQuarantinedItemsAsync();

        // Assert
        Assert.Single(items);
        Assert.Equal(currentMatchRow.Id, items[0].OutboxId);
    }

    [Fact]
    public async Task ApplyFailure_MissingSomeBatchRows_RollsBackEntireTransaction()
    {
        // Arrange
        var context = new TestProvisionedTerminalContext();
        using var db = CreateDbContext(context);
        var options = new SyncProcessorOptions { MaxRetryAttempts = 5 };
        var applier = new EfSyncAckApplier(db, context, options, NullLogger<EfSyncAckApplier>.Instance);

        var row1 = CreateTestOutbox(idempotencyKey: "idem-row1");
        row1.Status = SyncOutboxStatus.Pending;
        row1.AttemptCount = 0;

        var row2 = CreateTestOutbox(idempotencyKey: "idem-row2");
        row2.Status = SyncOutboxStatus.Pending;
        row2.AttemptCount = 0;

        db.SyncOutbox.Add(row1); // Add only row1 to DB, but omit row2 to simulate missing row
        await db.SaveChangesAsync();

        var item1 = ToBatchItem(row1);
        var item2 = ToBatchItem(row2);

        var ev1 = ToRequestEvent(item1, 1);
        var ev2 = ToRequestEvent(item2, 2);

        var batch = new SyncOutboxBatch(new[] { item1, item2 });
        var request = new SyncIngestRequest(1, 10, 20, 500, "idem", "hash", "corr", new[] { ev1, ev2 });
        var error = new SyncIngestClientError(SyncIngestClientErrorType.Timeout, "Request timed out", "TIMEOUT_ERROR");

        // Act
        var result = await applier.ApplyFailureAsync(batch, request, error);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("OUTBOX_ROWS_MISSING", result.ErrorCode);

        // Ensure row1 was NOT modified (attempt count remains 0, status remains Pending)
        var dbRow1 = await db.SyncOutbox.FindAsync(row1.Id);
        Assert.NotNull(dbRow1);
        Assert.Equal(SyncOutboxStatus.Pending, dbRow1.Status);
        Assert.Equal(0, dbRow1.AttemptCount);
    }

    [Fact]
    public async Task ApplyFailure_RowMetadataMismatch_RollsBackEntireTransaction()
    {
        // Arrange
        var context = new TestProvisionedTerminalContext();
        using var db = CreateDbContext(context);
        var options = new SyncProcessorOptions { MaxRetryAttempts = 5 };
        var applier = new EfSyncAckApplier(db, context, options, NullLogger<EfSyncAckApplier>.Instance);

        var row = CreateTestOutbox(idempotencyKey: "idem-correct");
        row.Status = SyncOutboxStatus.Pending;
        row.AttemptCount = 0;
        db.SyncOutbox.Add(row);
        await db.SaveChangesAsync();

        var item = ToBatchItem(row);
        var ev = ToRequestEvent(item, 600);

        // Create a mismatch event (different idempotency key)
        var mismatchEv = ev with { IdempotencyKey = "mismatch-idem-key" };

        var batch = new SyncOutboxBatch(new[] { item });
        var request = new SyncIngestRequest(1, 10, 20, 600, "idem", "hash", "corr", new[] { mismatchEv });
        var error = new SyncIngestClientError(SyncIngestClientErrorType.Timeout, "Request timed out", "TIMEOUT_ERROR");

        // Act
        var result = await applier.ApplyFailureAsync(batch, request, error);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("OUTBOX_ROW_MISMATCH", result.ErrorCode);

        // Ensure row was NOT modified
        var dbRow = await db.SyncOutbox.FindAsync(row.Id);
        Assert.NotNull(dbRow);
        Assert.Equal(SyncOutboxStatus.Pending, dbRow.Status);
        Assert.Equal(0, dbRow.AttemptCount);
    }
}
