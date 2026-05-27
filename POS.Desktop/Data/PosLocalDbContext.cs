using Microsoft.EntityFrameworkCore;
using POS.Desktop.Data.LocalEntities;
using POS.Shared.Contracts;

namespace POS.Desktop.Data;

public class PosLocalDbContext : DbContext
{
    private const int InvalidTenantId = 0;
    private const int InvalidLocationId = 0;
    private const int InvalidTerminalId = 0;
    private readonly IProvisionedTerminalContext _provisionedTerminalContext;

    private int CurrentTenantId => _provisionedTerminalContext.IsProvisioned ? _provisionedTerminalContext.CurrentTenantId : InvalidTenantId;

    private int CurrentLocationId => _provisionedTerminalContext.IsProvisioned ? _provisionedTerminalContext.CurrentLocationId : InvalidLocationId;

    private int CurrentTerminalId => _provisionedTerminalContext.IsProvisioned ? _provisionedTerminalContext.CurrentTerminalId : InvalidTerminalId;

    private bool IsProvisioned => _provisionedTerminalContext.IsProvisioned;

    public PosLocalDbContext(
        DbContextOptions<PosLocalDbContext> options,
        IProvisionedTerminalContext provisionedTerminalContext)
        : base(options)
    {
        _provisionedTerminalContext = provisionedTerminalContext;
    }

    public DbSet<SyncOutbox> SyncOutbox => Set<SyncOutbox>();

    public DbSet<PrintQueue> PrintQueue => Set<PrintQueue>();

    public DbSet<LocalRecoveryJournal> LocalRecoveryJournal => Set<LocalRecoveryJournal>();

    public DbSet<PaymentReconciliationQueue> PaymentReconciliationQueue => Set<PaymentReconciliationQueue>();

    public DbSet<SyncCursor> SyncCursors => Set<SyncCursor>();

    public DbSet<LocalRetentionState> LocalRetentionState => Set<LocalRetentionState>();

    public DbSet<TerminalProvisioning> TerminalProvisioning => Set<TerminalProvisioning>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PosLocalDbContext).Assembly);

        modelBuilder.Entity<SyncOutbox>()
            .HasQueryFilter(x => x.TenantId == CurrentTenantId);

        modelBuilder.Entity<PrintQueue>()
            .HasQueryFilter(x => x.TenantId == CurrentTenantId);

        modelBuilder.Entity<LocalRecoveryJournal>()
            .HasQueryFilter(x => x.TenantId == CurrentTenantId);

        modelBuilder.Entity<PaymentReconciliationQueue>()
            .HasQueryFilter(x => x.TenantId == CurrentTenantId);

        modelBuilder.Entity<SyncCursor>()
            .HasQueryFilter(x => x.TenantId == CurrentTenantId);

        modelBuilder.Entity<LocalRetentionState>()
            .HasQueryFilter(x => x.TenantId == CurrentTenantId);
    }
}
