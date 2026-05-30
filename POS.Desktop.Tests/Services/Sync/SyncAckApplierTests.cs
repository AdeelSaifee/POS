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
}
