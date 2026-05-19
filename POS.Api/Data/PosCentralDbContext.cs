using Microsoft.EntityFrameworkCore;
using POS.Shared.Contracts;
using POS.Shared.Domain.Entities.Central;

namespace POS.Api.Data;

public class PosCentralDbContext : DbContext
{
    private const int InvalidTenantId = 0;
    private readonly ICurrentTenantContext _currentTenantContext;

    private int CurrentTenantId => _currentTenantContext.HasTenant ? _currentTenantContext.CurrentTenantId : InvalidTenantId;

    private bool HasTenant => _currentTenantContext.HasTenant;

    public PosCentralDbContext(
        DbContextOptions<PosCentralDbContext> options,
        ICurrentTenantContext currentTenantContext)
        : base(options)
    {
        _currentTenantContext = currentTenantContext;
    }

    public DbSet<Company> Companies => Set<Company>();

    public DbSet<Location> Locations => Set<Location>();

    public DbSet<Terminal> Terminals => Set<Terminal>();

    public DbSet<TerminalSession> TerminalSessions => Set<TerminalSession>();

    public DbSet<Employee> Employees => Set<Employee>();

    public DbSet<EmployeeLocationRole> EmployeeLocationRoles => Set<EmployeeLocationRole>();

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<ReasonCode> ReasonCodes => Set<ReasonCode>();

    public DbSet<TenderMethod> TenderMethods => Set<TenderMethod>();

    public DbSet<TaxRule> TaxRules => Set<TaxRule>();

    public DbSet<ReceiptTemplate> ReceiptTemplates => Set<ReceiptTemplate>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<UnitOfMeasure> UnitsOfMeasure => Set<UnitOfMeasure>();

    public DbSet<Item> Items => Set<Item>();

    public DbSet<ItemVariant> ItemVariants => Set<ItemVariant>();

    public DbSet<ItemIdentifier> ItemIdentifiers => Set<ItemIdentifier>();

    public DbSet<PriceList> PriceLists => Set<PriceList>();

    public DbSet<CashAccount> CashAccounts => Set<CashAccount>();

    public DbSet<ItemPrice> ItemPrices => Set<ItemPrice>();

    public DbSet<ItemStock> ItemStocks => Set<ItemStock>();

    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();

    public DbSet<Shift> Shifts => Set<Shift>();

    public DbSet<CashDrawerMovement> CashDrawerMovements => Set<CashDrawerMovement>();

    public DbSet<ZReport> ZReports => Set<ZReport>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderLine> OrderLines => Set<OrderLine>();

    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<CashAccountMovement> CashAccountMovements => Set<CashAccountMovement>();

    public DbSet<ManagerAction> ManagerActions => Set<ManagerAction>();

    public DbSet<SyncIngestAck> SyncIngestAcks => Set<SyncIngestAck>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PosCentralDbContext).Assembly);

        modelBuilder.Entity<Company>()
            .HasQueryFilter(x => x.Id == CurrentTenantId && x.IsActive);

        modelBuilder.Entity<Employee>()
            .HasQueryFilter(x => x.TenantId == CurrentTenantId && x.IsActive);

        modelBuilder.Entity<Customer>()
            .HasQueryFilter(x => x.TenantId == CurrentTenantId && x.IsActive);

        modelBuilder.Entity<ReasonCode>()
            .HasQueryFilter(x => x.TenantId == CurrentTenantId && x.IsActive);

        modelBuilder.Entity<TenderMethod>()
            .HasQueryFilter(x => x.TenantId == CurrentTenantId && x.IsActive);

        modelBuilder.Entity<TaxRule>()
            .HasQueryFilter(x => x.TenantId == CurrentTenantId && x.IsActive);

        modelBuilder.Entity<ReceiptTemplate>()
            .HasQueryFilter(x => x.TenantId == CurrentTenantId && x.IsActive);

        modelBuilder.Entity<PriceList>()
            .HasQueryFilter(x => x.TenantId == CurrentTenantId && x.IsActive);

        modelBuilder.Entity<CashAccount>()
            .HasQueryFilter(x => x.TenantId == CurrentTenantId && x.IsActive);

        modelBuilder.Entity<Category>()
            .HasQueryFilter(x => x.TenantId == CurrentTenantId && x.IsActive);

        modelBuilder.Entity<UnitOfMeasure>()
            .HasQueryFilter(x => x.TenantId == CurrentTenantId && x.IsActive);

        modelBuilder.Entity<Location>()
            .HasQueryFilter(x => x.TenantId == CurrentTenantId && x.IsActive);

        modelBuilder.Entity<Terminal>()
            .HasQueryFilter(x => x.TenantId == CurrentTenantId && x.IsActive);

        modelBuilder.Entity<TerminalSession>()
            .HasQueryFilter(x => x.TenantId == CurrentTenantId && x.IsActive);

        modelBuilder.Entity<EmployeeLocationRole>()
            .HasQueryFilter(x => x.TenantId == CurrentTenantId && x.IsActive);

        modelBuilder.Entity<Item>()
            .HasQueryFilter(x => x.TenantId == CurrentTenantId && x.IsActive);

        modelBuilder.Entity<ItemVariant>()
            .HasQueryFilter(x => x.TenantId == CurrentTenantId && x.IsActive);

        modelBuilder.Entity<ItemIdentifier>()
            .HasQueryFilter(x => x.TenantId == CurrentTenantId && x.IsActive);

        modelBuilder.Entity<ItemPrice>()
            .HasQueryFilter(x => x.TenantId == CurrentTenantId && x.IsActive);

        modelBuilder.Entity<ItemStock>()
            .HasQueryFilter(x => x.TenantId == CurrentTenantId && x.IsActive);

        modelBuilder.Entity<Shift>()
            .HasQueryFilter(x => x.TenantId == CurrentTenantId);

        modelBuilder.Entity<Order>()
            .HasQueryFilter(x => x.TenantId == CurrentTenantId);

        modelBuilder.Entity<OrderLine>()
            .HasQueryFilter(x => x.TenantId == CurrentTenantId);

        modelBuilder.Entity<Payment>()
            .HasQueryFilter(x => x.TenantId == CurrentTenantId);

        modelBuilder.Entity<CashAccountMovement>()
            .HasQueryFilter(x => x.TenantId == CurrentTenantId);

        modelBuilder.Entity<ManagerAction>()
            .HasQueryFilter(x => x.TenantId == CurrentTenantId);

        modelBuilder.Entity<CashDrawerMovement>()
            .HasQueryFilter(x => x.TenantId == CurrentTenantId);

        modelBuilder.Entity<ZReport>()
            .HasQueryFilter(x => x.TenantId == CurrentTenantId);

        modelBuilder.Entity<InventoryMovement>()
            .HasQueryFilter(x => x.TenantId == CurrentTenantId);

        modelBuilder.Entity<SyncIngestAck>()
            .HasQueryFilter(x => x.TenantId == CurrentTenantId);
    }
}
