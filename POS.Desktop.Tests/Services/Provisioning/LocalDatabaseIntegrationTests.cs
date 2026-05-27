using System;
using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using POS.Desktop.Data;
using POS.Desktop.Data.LocalEntities;
using POS.Desktop.Services.Provisioning;
using POS.Shared.Contracts;
using POS.Shared.Enums;
using Xunit;

namespace POS.Desktop.Tests.Services.Provisioning;

public class LocalDatabaseIntegrationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<PosLocalDbContext> _options;

    public LocalDatabaseIntegrationTests()
    {
        // Set up in-memory SQLite connection
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
        var dbContext = new PosLocalDbContext(_options, context);
        dbContext.Database.EnsureCreated();
        return dbContext;
    }

    private SyncOutbox CreateTestOutbox(int tenantId, int locationId, int terminalId, long terminalSequence, string suffix)
    {
        return new SyncOutbox
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            LocationId = locationId,
            TerminalId = terminalId,
            BusinessDate = DateOnly.FromDateTime(DateTime.Today),
            TerminalSequence = terminalSequence,
            EventType = $"Sale_{suffix}",
            EventId = Guid.NewGuid(),
            PayloadJson = "{}",
            PayloadHash = $"hash_{suffix}",
            IdempotencyKey = $"key_{suffix}",
            CorrelationId = $"corr_{suffix}",
            Status = SyncOutboxStatus.Pending,
            AttemptCount = 0,
            IsActive = true,
            CreatedBy = "Tester",
            CreatedOn = DateTimeOffset.UtcNow
        };
    }

    [Fact]
    public void ProvisionedRead_ReturnsTenantScopedRows_WhenCurrentFilterUsesTenantOnly()
    {
        // Arrange
        // We set up a fully provisioned terminal context
        var record = new ProvisioningRecord(TenantId: 42, LocationId: 101, TerminalId: 999);
        var terminalContext = new ProvisionedTerminalContext(record);

        // Seeding database using a temporary fully provisioned context to allow inserts for different tenants
        using (var seedContext = CreateDbContext(terminalContext))
        {
            var match = CreateTestOutbox(tenantId: 42, locationId: 101, terminalId: 999, terminalSequence: 1, suffix: "match");
            var differentTenant = CreateTestOutbox(tenantId: 99, locationId: 101, terminalId: 999, terminalSequence: 2, suffix: "diffTenant");
            var differentLocation = CreateTestOutbox(tenantId: 42, locationId: 202, terminalId: 999, terminalSequence: 3, suffix: "diffLoc");

            seedContext.SyncOutbox.AddRange(match, differentTenant, differentLocation);
            seedContext.SaveChanges();
        }

        // Act & Assert
        // Re-read using the provisioned terminal context. Note: PosLocalDbContext currently
        // configures global query filters based only on TenantId, not LocationId or TerminalId.
        using (var readContext = CreateDbContext(terminalContext))
        {
            var results = readContext.SyncOutbox.ToList();

            // The global query filter scopes by TenantId only:
            // x => x.TenantId == CurrentTenantId (CurrentTenantId is 42)
            // Therefore, 'match' and 'differentLocation' should be returned since both belong to TenantId 42.
            // 'differentTenant' belongs to TenantId 99, so it should be filtered out.
            Assert.Equal(2, results.Count);
            Assert.Contains(results, x => x.EventType == "Sale_match");
            Assert.Contains(results, x => x.EventType == "Sale_diffLoc");
            Assert.DoesNotContain(results, x => x.EventType == "Sale_diffTenant");
        }
    }

    [Fact]
    public void UnprovisionedRead_ReturnsNoRows()
    {
        // Arrange
        // Seed some valid rows using a provisioned context first
        var seedRecord = new ProvisioningRecord(TenantId: 42, LocationId: 101, TerminalId: 999);
        var seedTerminalContext = new ProvisionedTerminalContext(seedRecord);

        using (var seedContext = CreateDbContext(seedTerminalContext))
        {
            var row1 = CreateTestOutbox(tenantId: 42, locationId: 101, terminalId: 999, terminalSequence: 1, suffix: "row1");
            var row2 = CreateTestOutbox(tenantId: 42, locationId: 101, terminalId: 999, terminalSequence: 2, suffix: "row2");

            seedContext.SyncOutbox.AddRange(row1, row2);
            seedContext.SaveChanges();
        }

        // Now configure the context as unprovisioned
        var unprovisionedContext = new ProvisionedTerminalContext(); // Defaults to unprovisioned state

        // Act
        using (var readContext = CreateDbContext(unprovisionedContext))
        {
            var results = readContext.SyncOutbox.ToList();

            // Assert
            // The query filter scopes by TenantId == CurrentTenantId, which is 0 for unprovisioned context.
            // No rows should be returned because all seeded rows have TenantId = 42.
            Assert.Empty(results);
        }
    }

    [Fact]
    public void HalfProvisionedRead_ReturnsNoRows()
    {
        // Arrange
        // Seed some valid rows using a provisioned context first
        var seedRecord = new ProvisioningRecord(TenantId: 42, LocationId: 101, TerminalId: 999);
        var seedTerminalContext = new ProvisionedTerminalContext(seedRecord);

        using (var seedContext = CreateDbContext(seedTerminalContext))
        {
            var row1 = CreateTestOutbox(tenantId: 42, locationId: 101, terminalId: 999, terminalSequence: 1, suffix: "row1");
            seedContext.SyncOutbox.Add(row1);
            seedContext.SaveChanges();
        }

        // Configure the context as half-provisioned (e.g. invalid/missing location)
        var halfProvisionedRecord = new ProvisioningRecord(TenantId: 42, LocationId: null, TerminalId: 999);
        var halfProvisionedContext = new ProvisionedTerminalContext(halfProvisionedRecord);

        // Act
        using (var readContext = CreateDbContext(halfProvisionedContext))
        {
            var results = readContext.SyncOutbox.ToList();

            // Assert
            // A half-provisioned context must fail closed, meaning IsProvisioned is false, and CurrentTenantId yields 0.
            // So no rows (even those with TenantId = 42) must be returned.
            Assert.False(halfProvisionedContext.IsProvisioned);
            Assert.Equal(0, halfProvisionedContext.CurrentTenantId);
            Assert.Empty(results);
        }
    }

    [Fact]
    public void DbContextScoping_SwitchingProvisioningState_AppliesCorrectFilters()
    {
        // Arrange
        var record = new ProvisioningRecord(TenantId: 42, LocationId: 101, TerminalId: 999);
        var terminalContext = new ProvisionedTerminalContext(record);

        using (var seedContext = CreateDbContext(terminalContext))
        {
            var row = CreateTestOutbox(tenantId: 42, locationId: 101, terminalId: 999, terminalSequence: 1, suffix: "row");
            seedContext.SyncOutbox.Add(row);
            seedContext.SaveChanges();
        }

        // Act & Assert 1: Query while provisioned
        using (var readContext1 = CreateDbContext(terminalContext))
        {
            var results1 = readContext1.SyncOutbox.ToList();
            Assert.Single(results1);
        }

        // Switch the terminal context state to unprovisioned
        terminalContext.UpdateState(ProvisioningRecord.Unprovisioned);

        // Act & Assert 2: Query using a new DbContext instance sharing the same updated context reference
        using (var readContext2 = CreateDbContext(terminalContext))
        {
            var results2 = readContext2.SyncOutbox.ToList();
            Assert.Empty(results2);
        }
    }
}
