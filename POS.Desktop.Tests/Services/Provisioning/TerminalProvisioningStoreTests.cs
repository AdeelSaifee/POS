using System;
using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using POS.Desktop.Data;
using POS.Desktop.Data.LocalEntities;
using POS.Desktop.Services.Provisioning;
using POS.Shared.Contracts;
using Xunit;

namespace POS.Desktop.Tests.Services.Provisioning;

public class TerminalProvisioningStoreTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<PosLocalDbContext> _options;

    public TerminalProvisioningStoreTests()
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

    private PosLocalDbContext CreateDbContext()
    {
        var stubContext = new NoProvisionedTerminalContext();
        var dbContext = new PosLocalDbContext(_options, stubContext);
        dbContext.Database.EnsureCreated();
        return dbContext;
    }

    [Fact]
    public void TerminalProvisioning_ModelProperties_AreNotSensitive()
    {
        // Arrange & Act
        var properties = typeof(TerminalProvisioning).GetProperties();
        var allowedNames = new[] { "Id", "TenantId", "LocationId", "TerminalId", "UpdatedAt" };

        // Assert
        foreach (var prop in properties)
        {
            Assert.Contains(prop.Name, allowedNames);

            // Explicitly assert that no sensitive fields exist
            Assert.NotEqual("PIN", prop.Name, StringComparer.OrdinalIgnoreCase);
            Assert.NotEqual("Password", prop.Name, StringComparer.OrdinalIgnoreCase);
            Assert.NotEqual("Token", prop.Name, StringComparer.OrdinalIgnoreCase);
            Assert.NotEqual("Card", prop.Name, StringComparer.OrdinalIgnoreCase);
            Assert.NotEqual("ConnectionString", prop.Name, StringComparer.OrdinalIgnoreCase);
            Assert.NotEqual("Secret", prop.Name, StringComparer.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void DatabaseSchema_Includes_TerminalProvisioningTable()
    {
        // Arrange & Act
        using var context = CreateDbContext();

        // Assert
        // We can check if we can query the table and it compiles/runs cleanly
        var count = context.TerminalProvisioning.Count();
        Assert.Equal(0, count);
    }

    [Fact]
    public void TerminalProvisioning_CanInsertAndRetrieveRecord()
    {
        // Arrange
        using var context = CreateDbContext();
        var now = DateTimeOffset.UtcNow;
        var provisioning = new TerminalProvisioning
        {
            Id = 1,
            TenantId = 42,
            LocationId = 101,
            TerminalId = 999,
            UpdatedAt = now
        };

        // Act
        context.TerminalProvisioning.Add(provisioning);
        context.SaveChanges();

        // Assert
        var retrieved = context.TerminalProvisioning.Single(x => x.Id == 1);
        Assert.Equal(42, retrieved.TenantId);
        Assert.Equal(101, retrieved.LocationId);
        Assert.Equal(999, retrieved.TerminalId);
        Assert.Equal(now, retrieved.UpdatedAt);
    }

    [Fact]
    public void TerminalProvisioning_EnforcesSingleRowPrimaryKeyConstraint()
    {
        // Arrange
        using (var context1 = CreateDbContext())
        {
            var row1 = new TerminalProvisioning { Id = 1, TenantId = 42 };
            context1.TerminalProvisioning.Add(row1);
            context1.SaveChanges();
        }

        // Act & Assert
        // Inserting a second row with the same Id (1) must throw a DB update exception (primary key violation)
        using (var context2 = CreateDbContext())
        {
            var row2 = new TerminalProvisioning { Id = 1, TenantId = 99 };
            context2.TerminalProvisioning.Add(row2);
            Assert.Throws<DbUpdateException>(() => context2.SaveChanges());
        }
    }

    [Fact]
    public void TerminalProvisioning_SupportsNullableValues_ForUnprovisionedOrHalfProvisionedStates()
    {
        // Arrange
        using var context = CreateDbContext();

        // Half-provisioned state record (e.g. location/terminal are null but tenant is set)
        var halfProvisioned = new TerminalProvisioning
        {
            Id = 1,
            TenantId = 42,
            LocationId = null,
            TerminalId = null,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        // Act
        context.TerminalProvisioning.Add(halfProvisioned);
        context.SaveChanges();

        // Assert
        var retrieved = context.TerminalProvisioning.Single(x => x.Id == 1);
        Assert.Equal(42, retrieved.TenantId);
        Assert.Null(retrieved.LocationId);
        Assert.Null(retrieved.TerminalId);
        Assert.NotNull(retrieved.UpdatedAt);
    }

    [Fact]
    public void TerminalProvisioning_RejectsRowsWithIdOtherThanOne()
    {
        // Arrange
        using var context = CreateDbContext();
        var row = new TerminalProvisioning
        {
            Id = 2, // Violates check constraint Id = 1
            TenantId = 42,
            LocationId = 101,
            TerminalId = 999
        };

        // Act & Assert
        context.TerminalProvisioning.Add(row);
        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void TerminalProvisioning_RejectsNonPositiveTenantId(int invalidTenantId)
    {
        // Arrange
        using var context = CreateDbContext();
        var row = new TerminalProvisioning
        {
            Id = 1,
            TenantId = invalidTenantId, // Violates check constraint TenantId > 0
            LocationId = 101,
            TerminalId = 999
        };

        // Act & Assert
        context.TerminalProvisioning.Add(row);
        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void TerminalProvisioning_RejectsNonPositiveLocationId(int invalidLocationId)
    {
        // Arrange
        using var context = CreateDbContext();
        var row = new TerminalProvisioning
        {
            Id = 1,
            TenantId = 42,
            LocationId = invalidLocationId, // Violates check constraint LocationId > 0
            TerminalId = 999
        };

        // Act & Assert
        context.TerminalProvisioning.Add(row);
        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void TerminalProvisioning_RejectsNonPositiveTerminalId(int invalidTerminalId)
    {
        // Arrange
        using var context = CreateDbContext();
        var row = new TerminalProvisioning
        {
            Id = 1,
            TenantId = 42,
            LocationId = 101,
            TerminalId = invalidTerminalId // Violates check constraint TerminalId > 0
        };

        // Act & Assert
        context.TerminalProvisioning.Add(row);
        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }
}
