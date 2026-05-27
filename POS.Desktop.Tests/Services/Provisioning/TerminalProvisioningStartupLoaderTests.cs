using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using POS.Desktop.Data;
using POS.Desktop.Data.LocalEntities;
using POS.Desktop.Services.Provisioning;
using POS.Shared.Contracts;
using Xunit;

namespace POS.Desktop.Tests.Services.Provisioning;

/// <summary>
/// Tests for <see cref="TerminalProvisioningStartupLoader"/> covering Task 4.2.7 and Task 4.2.8.
/// Verifies that persisted provisioning state is correctly loaded into the runtime context at
/// startup, and that provisioning survives a simulated application restart.
/// </summary>
public sealed class TerminalProvisioningStartupLoaderTests : IDisposable
{
    // Shared open connection for in-memory SQLite tests (keeps the DB alive for the test lifetime).
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<PosLocalDbContext> _options;

    public TerminalProvisioningStartupLoaderTests()
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

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private PosLocalDbContext CreateDbContext()
    {
        var stubContext = new NoProvisionedTerminalContext();
        var db = new PosLocalDbContext(_options, stubContext);
        db.Database.EnsureCreated();
        return db;
    }

    private TerminalProvisioningStartupLoader CreateLoader(ProvisionedTerminalContext runtimeContext)
    {
        var services = new ServiceCollection();
        services.AddDbContext<PosLocalDbContext>(opt => opt.UseSqlite(_connection));
        services.AddSingleton<IProvisionedTerminalContext>(runtimeContext);
        services.AddScoped<ITerminalProvisioningStore, EfTerminalProvisioningStore>();

        var provider = services.BuildServiceProvider();

        // Ensure schema exists (idempotent for in-memory DB already created).
        using var db = provider.GetRequiredService<PosLocalDbContext>();
        db.Database.EnsureCreated();

        return new TerminalProvisioningStartupLoader(
            provider.GetRequiredService<IServiceScopeFactory>(),
            runtimeContext,
            NullLogger<TerminalProvisioningStartupLoader>.Instance);
    }

    // -------------------------------------------------------------------------
    // Task 4.2.7 tests
    // -------------------------------------------------------------------------

    /// <summary>
    /// A fully-valid persisted provisioning row must update the runtime context.
    /// </summary>
    [Fact]
    public async Task StartupLoader_LoadsValidPersistedProvisioning_UpdatesRuntimeContext()
    {
        // Arrange – persist a fully-provisioned row
        using (var db = CreateDbContext())
        {
            db.TerminalProvisioning.Add(new TerminalProvisioning
            {
                Id = 1,
                TenantId = 10,
                LocationId = 20,
                TerminalId = 30,
                UpdatedAt = DateTimeOffset.UtcNow
            });
            db.SaveChanges();
        }

        var runtimeContext = new ProvisionedTerminalContext();
        var loader = CreateLoader(runtimeContext);

        // Act
        await loader.LoadAsync(CancellationToken.None);

        // Assert – context must be provisioned with the persisted values
        Assert.True(runtimeContext.IsProvisioned);
        Assert.Equal(10, runtimeContext.CurrentTenantId);
        Assert.Equal(20, runtimeContext.CurrentLocationId);
        Assert.Equal(30, runtimeContext.CurrentTerminalId);
    }

    /// <summary>
    /// When no row exists the runtime context must remain unprovisioned (fail-closed).
    /// </summary>
    [Fact]
    public async Task StartupLoader_NoPersistedRow_KeepsRuntimeContextUnprovisioned()
    {
        // Arrange – empty database (no provisioning row)
        using var db = CreateDbContext(); // creates schema only

        var runtimeContext = new ProvisionedTerminalContext();
        var loader = CreateLoader(runtimeContext);

        // Act
        await loader.LoadAsync(CancellationToken.None);

        // Assert – must remain fail-closed
        Assert.False(runtimeContext.IsProvisioned);
        Assert.Equal(0, runtimeContext.CurrentTenantId);
        Assert.Equal(0, runtimeContext.CurrentLocationId);
        Assert.Equal(0, runtimeContext.CurrentTerminalId);
    }

    /// <summary>
    /// A partial/half-provisioned row must not partially update the context; it must stay fail-closed.
    /// </summary>
    [Fact]
    public async Task StartupLoader_PartialPersistedRow_FailsClosed()
    {
        // Arrange – insert a partial row (TenantId set, but LocationId and TerminalId are null)
        using (var db = CreateDbContext())
        {
            db.TerminalProvisioning.Add(new TerminalProvisioning
            {
                Id = 1,
                TenantId = 42,
                LocationId = null,
                TerminalId = null,
                UpdatedAt = DateTimeOffset.UtcNow
            });
            db.SaveChanges();
        }

        var runtimeContext = new ProvisionedTerminalContext();
        var loader = CreateLoader(runtimeContext);

        // Act
        await loader.LoadAsync(CancellationToken.None);

        // Assert – partial IDs must not be exposed; context remains unprovisioned
        Assert.False(runtimeContext.IsProvisioned);
        Assert.Equal(0, runtimeContext.CurrentTenantId);
        Assert.Equal(0, runtimeContext.CurrentLocationId);
        Assert.Equal(0, runtimeContext.CurrentTerminalId);
    }

    // -------------------------------------------------------------------------
    // Task 4.2.8 tests – restart survival
    // -------------------------------------------------------------------------

    /// <summary>
    /// Simulates an application restart by using a temp SQLite file:
    /// 1. Session 1: persist a provisioning row and dispose all services.
    /// 2. Session 2: open the same file, run the startup loader.
    /// 3. Assert: the runtime context reflects the data from session 1.
    /// </summary>
    [Fact]
    public async Task ProvisioningSurvivesRestart_WhenNewHostLoadsSameDatabase()
    {
        // Use a temp file instead of :memory: to survive "restart" (service re-creation).
        var tempDbPath = Path.Combine(Path.GetTempPath(), $"pos_test_{Guid.NewGuid():N}.db");
        try
        {
            // --- Session 1: provision and dispose ---
            {
                var connectionString = $"Data Source={tempDbPath}";
                var options = new DbContextOptionsBuilder<PosLocalDbContext>()
                    .UseSqlite(connectionString)
                    .Options;

                var stubContext = new NoProvisionedTerminalContext();
                using var db1 = new PosLocalDbContext(options, stubContext);
                db1.Database.EnsureCreated();

                db1.TerminalProvisioning.Add(new TerminalProvisioning
                {
                    Id = 1,
                    TenantId = 7,
                    LocationId = 8,
                    TerminalId = 9,
                    UpdatedAt = DateTimeOffset.UtcNow
                });
                db1.SaveChanges();
                // db1 disposed here — simulates app shutdown
            }

            // --- Session 2: fresh startup loading same file ---
            {
                var connectionString = $"Data Source={tempDbPath}";

                var services = new ServiceCollection();
                services.AddDbContext<PosLocalDbContext>(opt => opt.UseSqlite(connectionString));

                var runtimeContext = new ProvisionedTerminalContext(); // starts unprovisioned
                services.AddSingleton<IProvisionedTerminalContext>(runtimeContext);
                services.AddScoped<ITerminalProvisioningStore, EfTerminalProvisioningStore>();

                var provider = services.BuildServiceProvider();

                var loader = new TerminalProvisioningStartupLoader(
                    provider.GetRequiredService<IServiceScopeFactory>(),
                    runtimeContext,
                    NullLogger<TerminalProvisioningStartupLoader>.Instance);

                // Act — simulate startup loader running after migrations
                await loader.LoadAsync(CancellationToken.None);

                // Assert — provisioning data from session 1 must be present in session 2 context
                Assert.True(runtimeContext.IsProvisioned);
                Assert.Equal(7, runtimeContext.CurrentTenantId);
                Assert.Equal(8, runtimeContext.CurrentLocationId);
                Assert.Equal(9, runtimeContext.CurrentTerminalId);

                await provider.DisposeAsync();
            }
        }
        finally
        {
            // Release all pooled SQLite connections before deleting the temp file.
            // Without this, the connection pool holds a file handle that blocks File.Delete.
            SqliteConnection.ClearAllPools();

            if (File.Exists(tempDbPath))
            {
                File.Delete(tempDbPath);
            }
        }
    }

    /// <summary>
    /// When the DB has no row, a stale provisioned runtime context (e.g. seeded from appsettings)
    /// must be explicitly reset to unprovisioned by the loader — not silently left active.
    /// </summary>
    [Fact]
    public async Task Loader_DoesNotUseBrowserStorageOrAppsettingsAsDurableTruth()
    {
        // Arrange – empty database (no provisioning row)
        using var db = CreateDbContext();

        // Simulate appsettings.json having seeded stale provisioning values into the context.
        var staleRecord = new ProvisioningRecord(99, 88, 77);
        var runtimeContext = new ProvisionedTerminalContext(staleRecord);

        // Confirm context starts "provisioned" from the stale seed.
        Assert.True(runtimeContext.IsProvisioned);

        var loader = CreateLoader(runtimeContext);

        // Act – loader runs and finds no row in the database.
        await loader.LoadAsync(CancellationToken.None);

        // Assert – the loader must actively reset to fail-closed because the durable SQLite
        // source has no row. The stale appsettings seed must not remain active.
        Assert.False(runtimeContext.IsProvisioned);
        Assert.Equal(0, runtimeContext.CurrentTenantId);
        Assert.Equal(0, runtimeContext.CurrentLocationId);
        Assert.Equal(0, runtimeContext.CurrentTerminalId);
    }

    /// <summary>
    /// When the DB row is partial/invalid and the runtime context was previously provisioned
    /// (e.g. from a prior session), the loader must explicitly reset the context to fail-closed.
    /// </summary>
    [Fact]
    public async Task StartupLoader_PartialPersistedRow_WithStaleContext_FailsClosed()
    {
        // Arrange – insert a partial row (only TenantId set)
        using (var db = CreateDbContext())
        {
            db.TerminalProvisioning.Add(new TerminalProvisioning
            {
                Id = 1,
                TenantId = 42,
                LocationId = null,
                TerminalId = null,
                UpdatedAt = DateTimeOffset.UtcNow
            });
            db.SaveChanges();
        }

        // Start with a context that is already provisioned (stale state from a prior session).
        var staleRecord = new ProvisioningRecord(42, 101, 999);
        var runtimeContext = new ProvisionedTerminalContext(staleRecord);
        Assert.True(runtimeContext.IsProvisioned, "Pre-condition: context should start provisioned.");

        var loader = CreateLoader(runtimeContext);

        // Act
        await loader.LoadAsync(CancellationToken.None);

        // Assert – partial row must reset the stale context to fail-closed.
        Assert.False(runtimeContext.IsProvisioned);
        Assert.Equal(0, runtimeContext.CurrentTenantId);
        Assert.Equal(0, runtimeContext.CurrentLocationId);
        Assert.Equal(0, runtimeContext.CurrentTerminalId);
    }

    /// <summary>
    /// When ITerminalProvisioningStore.GetProvisioningRecordAsync throws, the loader must catch the
    /// exception, reset any stale provisioned context to fail-closed, and not propagate the error.
    /// </summary>
    [Fact]
    public async Task StartupLoader_StoreThrows_FailsClosed()
    {
        // Arrange – start with a stale provisioned runtime context.
        var staleRecord = new ProvisioningRecord(5, 6, 7);
        var runtimeContext = new ProvisionedTerminalContext(staleRecord);
        Assert.True(runtimeContext.IsProvisioned, "Pre-condition: context must start provisioned.");

        // Build a scope factory that resolves a store which always throws.
        var services = new ServiceCollection();
        services.AddSingleton<IProvisionedTerminalContext>(runtimeContext);
        services.AddScoped<ITerminalProvisioningStore, ThrowingProvisioningStore>();
        var provider = services.BuildServiceProvider();

        var loader = new TerminalProvisioningStartupLoader(
            provider.GetRequiredService<IServiceScopeFactory>(),
            runtimeContext,
            NullLogger<TerminalProvisioningStartupLoader>.Instance);

        // Act – LoadAsync must not throw; the exception is caught internally.
        await loader.LoadAsync(CancellationToken.None);

        // Assert – stale context must be reset to fail-closed by the catch block.
        Assert.False(runtimeContext.IsProvisioned);
        Assert.Equal(0, runtimeContext.CurrentTenantId);
        Assert.Equal(0, runtimeContext.CurrentLocationId);
        Assert.Equal(0, runtimeContext.CurrentTerminalId);
    }

    /// <summary>
    /// A test stub for <see cref="ITerminalProvisioningStore"/> that always throws an
    /// <see cref="InvalidOperationException"/> from <see cref="GetProvisioningRecordAsync"/>.
    /// Used to exercise the catch block in <see cref="TerminalProvisioningStartupLoader.LoadAsync"/>.
    /// </summary>
    private sealed class ThrowingProvisioningStore : ITerminalProvisioningStore
    {
        public Task<ProvisioningRecord> GetProvisioningRecordAsync(CancellationToken cancellationToken)
            => throw new InvalidOperationException("Simulated SQLite read failure.");

        public Task<ProvisioningResult> ProvisionTerminalAsync(
            int tenantId, int locationId, int terminalId, CancellationToken cancellationToken)
            => throw new InvalidOperationException("Simulated SQLite write failure.");
    }
}
