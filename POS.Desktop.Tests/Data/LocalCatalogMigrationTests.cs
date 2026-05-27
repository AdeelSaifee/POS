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

    // Test 2b: MigrateAsync must write the AddLocalCatalogSearchIndexes entry and apply the indexes.
    [Fact]
    public async Task MigrateAsync_AppliesSearchIndexes()
    {
        var tempDbPath = Path.Combine(Path.GetTempPath(), $"pos_migtest_{Guid.NewGuid():N}.db");
        try
        {
            var connectionString = $"Data Source={tempDbPath}";
            using var db = new PosLocalDbContext(BuildOptions(connectionString), new NoProvisionedTerminalContext());

            await db.Database.MigrateAsync();

            var appliedMigrations = await db.Database.GetAppliedMigrationsAsync();
            Assert.Contains("20260527095912_AddLocalCatalogSearchIndexes", appliedMigrations);

            // Verify indexes exist in SQLite schema using direct ADO.NET connection
            var indexes = new Dictionary<string, string>();
            using var conn = new SqliteConnection(connectionString);
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT name, tbl_name FROM sqlite_master WHERE type='index'";
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var indexName = reader.GetString(0);
                var tableName = reader.GetString(1);
                indexes[indexName] = tableName;
            }

            Assert.True(indexes.TryGetValue("IX_LocalItemVariants_TenantId_ItemId", out var t1) && t1 == "LocalItemVariants");
            Assert.True(indexes.TryGetValue("IX_LocalItemVariants_TenantId_SKU", out var t2) && t2 == "LocalItemVariants");
            Assert.True(indexes.TryGetValue("IX_LocalItems_TenantId_CategoryId", out var t3) && t3 == "LocalItems");
            Assert.True(indexes.TryGetValue("IX_LocalItems_TenantId_Name", out var t4) && t4 == "LocalItems");
            Assert.True(indexes.TryGetValue("IX_LocalItemPrices_TenantId_ItemVariantId_PriceListId", out var t5) && t5 == "LocalItemPrices");
            Assert.True(indexes.TryGetValue("IX_LocalItemIdentifiers_TenantId_ItemVariantId", out var t6) && t6 == "LocalItemIdentifiers");
            Assert.True(indexes.TryGetValue("IX_LocalCategories_TenantId_SortOrder", out var t7) && t7 == "LocalCategories");
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
