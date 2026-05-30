using System;
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
using POS.Shared.Enums;
using Xunit;

namespace POS.Desktop.Tests.Services.Sync;

/// <summary>
/// Database integration and logic verification tests for <see cref="EfSyncStatusService"/>.
/// </summary>
public sealed class SyncStatusServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<PosLocalDbContext> _options;

    public SyncStatusServiceTests()
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

    private sealed class FakeSyncConnectivityService : ISyncConnectivityService
    {
        public bool Connected { get; set; } = true;

        public Task<bool> IsConnectedAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Connected);
        }
    }

    private SyncOutbox CreateTestOutbox(
        int tenantId,
        int locationId,
        int terminalId,
        long terminalSequence,
        SyncOutboxStatus status,
        string? lastErrorCode = null,
        bool isActive = true)
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
            PayloadJson = "{}",
            PayloadHash = "hash",
            IdempotencyKey = $"key-{Guid.NewGuid()}",
            CorrelationId = "corr-123",
            Status = status,
            LastErrorCode = lastErrorCode,
            IsActive = isActive,
            CreatedBy = "Operator",
            CreatedOn = DateTimeOffset.UtcNow
        };
    }

    private PaymentReconciliationQueue CreateTestReconcileQueue(
        int tenantId,
        int locationId,
        int terminalId,
        PaymentReconciliationStatus status,
        bool isActive = true)
    {
        return new PaymentReconciliationQueue
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            LocationId = locationId,
            TerminalId = terminalId,
            OrderId = Guid.NewGuid(),
            PaymentId = Guid.NewGuid(),
            TenderMethodId = 1,
            Status = status,
            IdempotencyKey = $"key-{Guid.NewGuid()}",
            IsActive = isActive,
            CreatedBy = "Operator",
            CreatedOn = DateTimeOffset.UtcNow
        };
    }

    private LocalRecoveryJournal CreateTestRecoveryJournal(
        int tenantId,
        int locationId,
        int terminalId,
        RecoveryJournalStatus status,
        RecoveryType recoveryType,
        bool isActive = true)
    {
        return new LocalRecoveryJournal
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            LocationId = locationId,
            TerminalId = terminalId,
            Status = status,
            RecoveryType = recoveryType,
            StatePayloadJson = "{}",
            IdempotencyKey = $"key-{Guid.NewGuid()}",
            IsActive = isActive,
            CreatedBy = "Operator",
            CreatedOn = DateTimeOffset.UtcNow
        };
    }

    [Fact]
    public async Task GetStatusAsync_WhenUnprovisioned_ReturnsSafeZeroedStatus()
    {
        // Arrange
        var context = new TestProvisionedTerminalContext { IsProvisioned = false };
        using var db = CreateDbContext(context);

        // Seed some data to make sure it is NOT read/leaked
        db.SyncOutbox.Add(CreateTestOutbox(1, 10, 20, 1, SyncOutboxStatus.Pending));
        await db.SaveChangesAsync();

        var connectivity = new FakeSyncConnectivityService { Connected = true };
        var service = new EfSyncStatusService(db, context, connectivity, NullLogger<EfSyncStatusService>.Instance);

        // Act
        var status = await service.GetStatusAsync();

        // Assert
        Assert.False(status.IsProvisioned);
        Assert.True(status.IsOnline); // Online is still computed cheaply
        Assert.Equal(0, status.PendingOutboxCount);
        Assert.Equal(0, status.FailedOutboxCount);
        Assert.Equal(0, status.DeadLetterOutboxCount);
        Assert.Equal(0, status.RetryableOutboxCount);
        Assert.Equal(0, status.PendingReconciliationCount);
        Assert.Equal(0, status.OpenRecoveryJournalCount);
        Assert.Null(status.LastPushedChunkSequence);
        Assert.Null(status.LastAckedChunkSequence);
        Assert.Null(status.LastAckedOn);
        Assert.Null(status.LastErrorCode);
    }

    [Fact]
    public async Task GetStatusAsync_WhenProvisioned_CalculatesCorrectCountsAndRetryable()
    {
        // Arrange
        var context = new TestProvisionedTerminalContext { IsProvisioned = true };
        using var db = CreateDbContext(context);

        db.SyncOutbox.Add(CreateTestOutbox(1, 10, 20, 1, SyncOutboxStatus.Pending));
        db.SyncOutbox.Add(CreateTestOutbox(1, 10, 20, 2, SyncOutboxStatus.Pending));
        db.SyncOutbox.Add(CreateTestOutbox(1, 10, 20, 3, SyncOutboxStatus.Failed));
        db.SyncOutbox.Add(CreateTestOutbox(1, 10, 20, 4, SyncOutboxStatus.DeadLetter));
        db.SyncOutbox.Add(CreateTestOutbox(1, 10, 20, 5, SyncOutboxStatus.Acked)); // Acked not counted

        db.PaymentReconciliationQueue.Add(CreateTestReconcileQueue(1, 10, 20, PaymentReconciliationStatus.Pending));
        db.PaymentReconciliationQueue.Add(CreateTestReconcileQueue(1, 10, 20, PaymentReconciliationStatus.ResolvedCaptured)); // Resolved not counted

        db.LocalRecoveryJournal.Add(CreateTestRecoveryJournal(1, 10, 20, RecoveryJournalStatus.Open, RecoveryType.SyncInFlight));
        db.LocalRecoveryJournal.Add(CreateTestRecoveryJournal(1, 10, 20, RecoveryJournalStatus.Resolved, RecoveryType.SyncInFlight)); // Resolved not counted

        await db.SaveChangesAsync();

        var connectivity = new FakeSyncConnectivityService { Connected = true };
        var service = new EfSyncStatusService(db, context, connectivity, NullLogger<EfSyncStatusService>.Instance);

        // Act
        var status = await service.GetStatusAsync();

        // Assert
        Assert.True(status.IsProvisioned);
        Assert.True(status.IsOnline);
        Assert.Equal(2, status.PendingOutboxCount);
        Assert.Equal(1, status.FailedOutboxCount);
        Assert.Equal(1, status.DeadLetterOutboxCount);
        Assert.Equal(3, status.RetryableOutboxCount); // 2 Pending + 1 Failed
        Assert.Equal(1, status.PendingReconciliationCount);
        Assert.Equal(1, status.OpenRecoveryJournalCount);
    }

    [Fact]
    public async Task GetStatusAsync_FiltersByLocationAndTerminal()
    {
        // Arrange
        var context = new TestProvisionedTerminalContext { IsProvisioned = true, CurrentLocationId = 10, CurrentTerminalId = 20 };
        using var db = CreateDbContext(context);

        // Match context
        db.SyncOutbox.Add(CreateTestOutbox(1, 10, 20, 1, SyncOutboxStatus.Pending));

        // Mismatched location/terminal
        db.SyncOutbox.Add(CreateTestOutbox(1, 11, 20, 2, SyncOutboxStatus.Pending));
        db.SyncOutbox.Add(CreateTestOutbox(1, 10, 21, 3, SyncOutboxStatus.Pending));

        await db.SaveChangesAsync();

        var connectivity = new FakeSyncConnectivityService { Connected = true };
        var service = new EfSyncStatusService(db, context, connectivity, NullLogger<EfSyncStatusService>.Instance);

        // Act
        var status = await service.GetStatusAsync();

        // Assert
        Assert.Equal(1, status.PendingOutboxCount);
    }

    [Fact]
    public async Task GetStatusAsync_RetrievesCursorSequencesAndAckedOn()
    {
        // Arrange
        var context = new TestProvisionedTerminalContext { IsProvisioned = true, CurrentLocationId = 10, CurrentTerminalId = 20 };
        using var db = CreateDbContext(context);

        var now = DateTimeOffset.UtcNow;
        db.SyncCursors.Add(new SyncCursor
        {
            Id = Guid.NewGuid(),
            TenantId = 1,
            LocationId = 10,
            TerminalId = 20,
            StreamName = "push:outbox",
            LastPushedChunkSequence = 15,
            LastAckedChunkSequence = 12,
            Status = SyncCursorStatus.Active,
            IsActive = true,
            CreatedBy = "Test",
            CreatedOn = now,
            UpdatedOn = now
        });

        // Seed an Acked outbox row with AckedOn set to represent LastAckedOn
        var ackedTime = now.AddMinutes(-5);
        var ackedOutbox = CreateTestOutbox(1, 10, 20, 10, SyncOutboxStatus.Acked);
        ackedOutbox.AckedOn = ackedTime;
        db.SyncOutbox.Add(ackedOutbox);

        await db.SaveChangesAsync();

        var connectivity = new FakeSyncConnectivityService { Connected = true };
        var service = new EfSyncStatusService(db, context, connectivity, NullLogger<EfSyncStatusService>.Instance);

        // Act
        var status = await service.GetStatusAsync();

        // Assert
        Assert.Equal(15, status.LastPushedChunkSequence);
        Assert.Equal(12, status.LastAckedChunkSequence);
        Assert.Equal(ackedTime, status.LastAckedOn); // Verify LastAckedOn is populated from Acked outbox row
    }

    [Fact]
    public async Task GetStatusAsync_RetrievesFallbackErrorCode_WhenCursorHasNoError()
    {
        // Arrange
        var context = new TestProvisionedTerminalContext { IsProvisioned = true, CurrentLocationId = 10, CurrentTerminalId = 20 };
        using var db = CreateDbContext(context);

        // Cursor has no error
        db.SyncCursors.Add(new SyncCursor
        {
            Id = Guid.NewGuid(),
            TenantId = 1,
            LocationId = 10,
            TerminalId = 20,
            StreamName = "push:outbox",
            LastPushedChunkSequence = 15,
            LastAckedChunkSequence = 12,
            Status = SyncCursorStatus.Active,
            LastErrorCode = null,
            IsActive = true,
            CreatedBy = "Test",
            CreatedOn = DateTimeOffset.UtcNow
        });

        // Failed outbox has error
        db.SyncOutbox.Add(CreateTestOutbox(1, 10, 20, 1, SyncOutboxStatus.Failed, "OUTBOX_ERR_401"));

        await db.SaveChangesAsync();

        var connectivity = new FakeSyncConnectivityService { Connected = true };
        var service = new EfSyncStatusService(db, context, connectivity, NullLogger<EfSyncStatusService>.Instance);

        // Act
        var status = await service.GetStatusAsync();

        // Assert
        Assert.Equal("OUTBOX_ERR_401", status.LastErrorCode);
    }
}
