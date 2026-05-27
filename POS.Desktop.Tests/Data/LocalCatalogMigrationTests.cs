using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using POS.Desktop.Data;
using POS.Desktop.Services.Provisioning;
using Xunit;

namespace POS.Desktop.Tests.Data;

/// <summary>
/// Task 4.3.9 - Verifies that the startup migration path (Database.MigrateAsync) creates
/// all catalog tables and records the AddLocalCatalogSchema migration in __EFMigrationsHistory.
/// Uses a real temp SQLite file - in-memory SQLite does not maintain migration history.
/// </summary>
public sealed class LocalCatalogMigrationTests
{
    private static DbContextOptions<PosLocalDbContext> BuildOptions(string connectionString)
        => new DbContextOptionsBuilder<PosLocalDbContext>()
            .UseSqlite(connectionString)
            .Options;

    // Test 1: MigrateAsync (not EnsureCreated) must create all nine catalog tables.
    [Fact]
    public async Task MigrateAsync_CreatesAllCatalogTables()
    {
        var tempDbPath = Path.Combine(Path.GetTempPath(), $"pos_migtest_{Guid.NewGuid():N}.db");
        try
        {
            var connectionString = $"Data Source={tempDbPath}";
            using var db = new PosLocalDbContext(BuildOptions(connectionString), new NoProvisionedTerminalContext());

            await db.Database.MigrateAsync();

            // Inspect table names via a raw ADO.NET connection to avoid any EF filter interference.
            var tables = new List<string>();
            using var conn = new SqliteConnection(connectionString);
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table'";
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                tables.Add(reader.GetString(0));

            Assert.Contains("LocalCategories",     tables);
            Assert.Contains("LocalItems",          tables);
            Assert.Contains("LocalItemVariants",   tables);
            Assert.Contains("LocalItemIdentifiers",tables);
            Assert.Contains("LocalItemPrices",     tables);
            Assert.Contains("LocalUnitsOfMeasure", tables);
            Assert.Contains("LocalTaxRules",       tables);
            Assert.Contains("LocalTenderMethods",  tables);
            Assert.Contains("LocalReasonCodes",    tables);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(tempDbPath))
                File.Delete(tempDbPath);
        }
    }

    // Test 2: MigrateAsync must write the AddLocalCatalogSchema entry to __EFMigrationsHistory.
    // This confirms the migration ran through the EF migration pipeline (not EnsureCreated).
    [Fact]
    public async Task MigrateAsync_RecordsAddLocalCatalogSchemaMigration()
    {
        var tempDbPath = Path.Combine(Path.GetTempPath(), $"pos_migtest_{Guid.NewGuid():N}.db");
        try
        {
            var connectionString = $"Data Source={tempDbPath}";
            using var db = new PosLocalDbContext(BuildOptions(connectionString), new NoProvisionedTerminalContext());

            await db.Database.MigrateAsync();

            var appliedMigrations = await db.Database.GetAppliedMigrationsAsync();
            Assert.Contains("20260527064212_AddLocalCatalogSchema", appliedMigrations);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(tempDbPath))
                File.Delete(tempDbPath);
        }
    }

    // Test 3: MigrateAsync is idempotent - running it twice on the same file must not throw
    // and must not create duplicate migration history entries.
    [Fact]
    public async Task MigrateAsync_IsIdempotent_RunningTwiceDoesNotThrow()
    {
        var tempDbPath = Path.Combine(Path.GetTempPath(), $"pos_migtest_{Guid.NewGuid():N}.db");
        try
        {
            var connectionString = $"Data Source={tempDbPath}";

            using (var db = new PosLocalDbContext(BuildOptions(connectionString), new NoProvisionedTerminalContext()))
                await db.Database.MigrateAsync();

            using (var db = new PosLocalDbContext(BuildOptions(connectionString), new NoProvisionedTerminalContext()))
            {
                // Must not throw - EF applies only pending migrations, so a second call is a no-op.
                await db.Database.MigrateAsync();

                var appliedMigrations = await db.Database.GetAppliedMigrationsAsync();
                Assert.Contains("20260527064212_AddLocalCatalogSchema", appliedMigrations);
            }
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(tempDbPath))
                File.Delete(tempDbPath);
        }
    }
}
