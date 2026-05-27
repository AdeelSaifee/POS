using System;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using POS.Desktop.Data;
using POS.Desktop.Services.Provisioning;
using POS.Shared.Contracts;

namespace POS.Desktop.Tests.TestSupport;

/// <summary>
/// A reusable SQLite in-memory test database harness for PosLocalDbContext tests.
/// Manages the SqliteConnection lifetime and schema creation.
/// </summary>
public sealed class SqliteTestDatabase : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<PosLocalDbContext> _options;

    public SqliteTestDatabase()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<PosLocalDbContext>()
            .UseSqlite(_connection)
            .Options;
    }

    /// <summary>
    /// Creates a PosLocalDbContext instance using the in-memory SQLite connection.
    /// Ensures the database schema is created.
    /// </summary>
    public PosLocalDbContext CreateDbContext(IProvisionedTerminalContext? terminalContext = null)
    {
        var context = terminalContext ?? new NoProvisionedTerminalContext();
        var db = new PosLocalDbContext(_options, context);
        db.Database.EnsureCreated();
        return db;
    }

    /// <summary>
    /// Creates a PosLocalDbContext with a provisioned context for the given tenant.
    /// </summary>
    public PosLocalDbContext CreateProvisionedDbContext(int tenantId, int locationId = 101, int terminalId = 999)
    {
        var record = new ProvisioningRecord(tenantId, locationId, terminalId);
        var context = new ProvisionedTerminalContext(record);
        return CreateDbContext(context);
    }

    /// <summary>
    /// Creates a PosLocalDbContext with an unprovisioned context.
    /// </summary>
    public PosLocalDbContext CreateUnprovisionedDbContext()
    {
        var context = new ProvisionedTerminalContext(); // defaults to unprovisioned
        return CreateDbContext(context);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
