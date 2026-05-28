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

    public DbSet<LocalCategory> LocalCategories => Set<LocalCategory>();

    public DbSet<LocalItem> LocalItems => Set<LocalItem>();

    public DbSet<LocalItemVariant> LocalItemVariants => Set<LocalItemVariant>();

    public DbSet<LocalItemIdentifier> LocalItemIdentifiers => Set<LocalItemIdentifier>();

    public DbSet<LocalItemPrice> LocalItemPrices => Set<LocalItemPrice>();

    public DbSet<LocalUnitOfMeasure> LocalUnitsOfMeasure => Set<LocalUnitOfMeasure>();

    public DbSet<LocalTaxRule> LocalTaxRules => Set<LocalTaxRule>();

    public DbSet<LocalTenderMethod> LocalTenderMethods => Set<LocalTenderMethod>();

    public DbSet<LocalReasonCode> LocalReasonCodes => Set<LocalReasonCode>();

    public DbSet<LocalEmployee> LocalEmployees => Set<LocalEmployee>();

    public DbSet<LocalEmployeeLocationRole> LocalEmployeeLocationRoles => Set<LocalEmployeeLocationRole>();

    public DbSet<LocalTerminalSession> LocalTerminalSessions => Set<LocalTerminalSession>();

    public DbSet<LocalShift> LocalShifts => Set<LocalShift>();

    public DbSet<LocalOrder> LocalOrders => Set<LocalOrder>();

    public DbSet<LocalOrderLine> LocalOrderLines => Set<LocalOrderLine>();

    public DbSet<LocalPayment> LocalPayments => Set<LocalPayment>();

    public DbSet<LocalCashDrawerMovement> LocalCashDrawerMovements => Set<LocalCashDrawerMovement>();

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

        modelBuilder.Entity<LocalCategory>()
            .HasQueryFilter(x => x.TenantId == CurrentTenantId);

        modelBuilder.Entity<LocalItem>()
            .HasQueryFilter(x => x.TenantId == CurrentTenantId);

        modelBuilder.Entity<LocalItemVariant>()
            .HasQueryFilter(x => x.TenantId == CurrentTenantId);

        modelBuilder.Entity<LocalItemIdentifier>()
            .HasQueryFilter(x => x.TenantId == CurrentTenantId);

        modelBuilder.Entity<LocalItemPrice>()
            .HasQueryFilter(x => x.TenantId == CurrentTenantId);

        modelBuilder.Entity<LocalUnitOfMeasure>()
            .HasQueryFilter(x => x.TenantId == CurrentTenantId);

        modelBuilder.Entity<LocalTaxRule>()
            .HasQueryFilter(x => x.TenantId == CurrentTenantId);

        modelBuilder.Entity<LocalTenderMethod>()
            .HasQueryFilter(x => x.TenantId == CurrentTenantId);

        modelBuilder.Entity<LocalReasonCode>()
            .HasQueryFilter(x => x.TenantId == CurrentTenantId);

        modelBuilder.Entity<LocalEmployee>()
            .HasQueryFilter(x => x.TenantId == CurrentTenantId);

        modelBuilder.Entity<LocalEmployeeLocationRole>()
            .HasQueryFilter(x => x.TenantId == CurrentTenantId);

        modelBuilder.Entity<LocalTerminalSession>()
            .HasQueryFilter(x => x.TenantId == CurrentTenantId);

        modelBuilder.Entity<LocalShift>()
            .HasQueryFilter(x => x.TenantId == CurrentTenantId);

        modelBuilder.Entity<LocalOrder>()
            .HasQueryFilter(x => x.TenantId == CurrentTenantId);

        modelBuilder.Entity<LocalOrderLine>()
            .HasQueryFilter(x => x.TenantId == CurrentTenantId);

        modelBuilder.Entity<LocalPayment>()
            .HasQueryFilter(x => x.TenantId == CurrentTenantId);

        modelBuilder.Entity<LocalCashDrawerMovement>()
            .HasQueryFilter(x => x.TenantId == CurrentTenantId);
    }
}
