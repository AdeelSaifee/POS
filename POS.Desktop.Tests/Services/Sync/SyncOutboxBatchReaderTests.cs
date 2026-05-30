using System;
using System.Linq;
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
/// Database integration and selection logic tests for <see cref="EfSyncOutboxBatchReader"/>.
/// </summary>
public sealed class SyncOutboxBatchReaderTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<PosLocalDbContext> _options;

    public SyncOutboxBatchReaderTests()
    {
        // Set up in-memory SQLite connection to test database operations natively
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
        int tenantId,
        int locationId,
        int terminalId,
        long terminalSequence,
        SyncOutboxStatus status,
        bool isActive = true,
        DateOnly? businessDate = null)
    {
        return new SyncOutbox
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            LocationId = locationId,
            TerminalId = terminalId,
            BusinessDate = businessDate ?? new DateOnly(2026, 5, 30),
            TerminalSequence = terminalSequence,
            EventType = "OrderCompleted",
            EventId = Guid.NewGuid(),
            PayloadJson = "{}",
            PayloadHash = "hash",
            IdempotencyKey = $"key-{Guid.NewGuid()}",
            CorrelationId = "corr-123",
            Status = status,
            IsActive = isActive,
            CreatedBy = "Operator",
            CreatedOn = DateTimeOffset.UtcNow
        };
    }

    [Fact]
    public async Task SyncOutboxBatchReader_ReturnsEmpty_WhenTerminalUnprovisioned()
    {
        // Arrange
        var context = new TestProvisionedTerminalContext { IsProvisioned = false };
        using var db = CreateDbContext(context);

        db.SyncOutbox.Add(CreateTestOutbox(1, 10, 20, 1, SyncOutboxStatus.Pending));
        await db.SaveChangesAsync();

        var reader = new EfSyncOutboxBatchReader(db, context, new SyncProcessorOptions(), NullLogger<EfSyncOutboxBatchReader>.Instance);

        // Act
        var batch = await reader.ReadPendingBatchAsync();

        // Assert
        Assert.False(batch.HasItems);
        Assert.Equal(0, batch.Count);
        Assert.Empty(batch.Items);
    }

    [Fact]
    public async Task SyncOutboxBatchReader_ReturnsEmpty_WhenNoPendingRowsExist()
    {
        // Arrange
        var context = new TestProvisionedTerminalContext();
        using var db = CreateDbContext(context);

        var reader = new EfSyncOutboxBatchReader(db, context, new SyncProcessorOptions(), NullLogger<EfSyncOutboxBatchReader>.Instance);

        // Act
        var batch = await reader.ReadPendingBatchAsync();

        // Assert
        Assert.False(batch.HasItems);
        Assert.Equal(0, batch.Count);
    }

    [Fact]
    public async Task SyncOutboxBatchReader_SelectsOnlyPendingActiveRows_ForCurrentLocationAndTerminal()
    {
        // Arrange
        var context = new TestProvisionedTerminalContext();
        using var db = CreateDbContext(context);

        var matchingPending = CreateTestOutbox(1, 10, 20, 1, SyncOutboxStatus.Pending);
        var wrongLocation = CreateTestOutbox(1, 99, 20, 2, SyncOutboxStatus.Pending);
        var wrongTerminal = CreateTestOutbox(1, 10, 99, 3, SyncOutboxStatus.Pending);
        var inactiveMatching = CreateTestOutbox(1, 10, 20, 4, SyncOutboxStatus.Pending, isActive: false);

        db.SyncOutbox.AddRange(matchingPending, wrongLocation, wrongTerminal, inactiveMatching);
        await db.SaveChangesAsync();

        var reader = new EfSyncOutboxBatchReader(db, context, new SyncProcessorOptions(), NullLogger<EfSyncOutboxBatchReader>.Instance);

        // Act
        var batch = await reader.ReadPendingBatchAsync();

        // Assert
        Assert.True(batch.HasItems);
        Assert.Equal(1, batch.Count);
        Assert.Equal(matchingPending.Id, batch.Items[0].Id);
    }

    [Fact]
    public async Task SyncOutboxBatchReader_ExcludesAckedFailedInFlightDeadLetterRows()
    {
        // Arrange
        var context = new TestProvisionedTerminalContext();
        using var db = CreateDbContext(context);

        var pending = CreateTestOutbox(1, 10, 20, 1, SyncOutboxStatus.Pending);
        var acked = CreateTestOutbox(1, 10, 20, 2, SyncOutboxStatus.Acked);
        var failed = CreateTestOutbox(1, 10, 20, 3, SyncOutboxStatus.Failed);
        var inFlight = CreateTestOutbox(1, 10, 20, 4, SyncOutboxStatus.InFlight);
        var deadLetter = CreateTestOutbox(1, 10, 20, 5, SyncOutboxStatus.DeadLetter);

        db.SyncOutbox.AddRange(pending, acked, failed, inFlight, deadLetter);
        await db.SaveChangesAsync();

        var reader = new EfSyncOutboxBatchReader(db, context, new SyncProcessorOptions(), NullLogger<EfSyncOutboxBatchReader>.Instance);

        // Act
        var batch = await reader.ReadPendingBatchAsync();

        // Assert
        Assert.Equal(1, batch.Count);
        Assert.Equal(pending.Id, batch.Items[0].Id);
    }

    [Fact]
    public async Task SyncOutboxBatchReader_OrdersByBusinessDateThenTerminalSequenceThenId()
    {
        // Arrange
        var context = new TestProvisionedTerminalContext();
        using var db = CreateDbContext(context);

        var dateLater = new DateOnly(2026, 6, 1);
        var dateEarlier = new DateOnly(2026, 5, 30);

        var item3 = CreateTestOutbox(1, 10, 20, 5, SyncOutboxStatus.Pending, businessDate: dateLater);
        var item2 = CreateTestOutbox(1, 10, 20, 10, SyncOutboxStatus.Pending, businessDate: dateEarlier);
        var item1 = CreateTestOutbox(1, 10, 20, 2, SyncOutboxStatus.Pending, businessDate: dateEarlier);

        db.SyncOutbox.AddRange(item3, item2, item1);
        await db.SaveChangesAsync();

        var reader = new EfSyncOutboxBatchReader(db, context, new SyncProcessorOptions(), NullLogger<EfSyncOutboxBatchReader>.Instance);

        // Act
        var batch = await reader.ReadPendingBatchAsync();

        // Assert
        Assert.Equal(3, batch.Count);
        Assert.Equal(item1.Id, batch.Items[0].Id); // Earlier date, lower seq
        Assert.Equal(item2.Id, batch.Items[1].Id); // Earlier date, higher seq
        Assert.Equal(item3.Id, batch.Items[2].Id); // Later date
    }

    [Fact]
    public async Task SyncOutboxBatchReader_RespectsBatchSize()
    {
        // Arrange
        var context = new TestProvisionedTerminalContext();
        using var db = CreateDbContext(context);

        var item1 = CreateTestOutbox(1, 10, 20, 1, SyncOutboxStatus.Pending);
        var item2 = CreateTestOutbox(1, 10, 20, 2, SyncOutboxStatus.Pending);
        var item3 = CreateTestOutbox(1, 10, 20, 3, SyncOutboxStatus.Pending);

        db.SyncOutbox.AddRange(item1, item2, item3);
        await db.SaveChangesAsync();

        var options = new SyncProcessorOptions { BatchSize = 2 };
        var reader = new EfSyncOutboxBatchReader(db, context, options, NullLogger<EfSyncOutboxBatchReader>.Instance);

        // Act
        var batch = await reader.ReadPendingBatchAsync();

        // Assert
        Assert.Equal(2, batch.Count);
        Assert.Equal(item1.Id, batch.Items[0].Id);
        Assert.Equal(item2.Id, batch.Items[1].Id);
    }
}
