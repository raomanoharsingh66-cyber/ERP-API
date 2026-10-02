using System.Reflection;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Domain.Common;
using BizFlow.Domain.Entities;
using BizFlow.Domain.Entities.ClothHub;
using Microsoft.EntityFrameworkCore;

namespace BizFlow.Infrastructure.Data;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
        : base(options)
    {
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public DbSet<Business> Businesses => Set<Business>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    // Phase 3 Inventory & Merchandise
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<UnitOfMeasure> UnitsOfMeasure => Set<UnitOfMeasure>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<InventoryStock> InventoryStocks => Set<InventoryStock>();
    public DbSet<StockTransaction> StockTransactions => Set<StockTransaction>();

    // Phase 4 Commercial & Sales
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<SalesOrder> SalesOrders => Set<SalesOrder>();
    public DbSet<SalesOrderItem> SalesOrderItems => Set<SalesOrderItem>();
    public DbSet<SalesInvoice> SalesInvoices => Set<SalesInvoice>();
    public DbSet<SalesInvoiceItem> SalesInvoiceItems => Set<SalesInvoiceItem>();
    public DbSet<SalesPayment> SalesPayments => Set<SalesPayment>();

    // Phase 5 Procurement & Vendor Payables
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();
    public DbSet<GoodsReceiptNote> GoodsReceiptNotes => Set<GoodsReceiptNote>();
    public DbSet<GoodsReceiptNoteItem> GoodsReceiptNoteItems => Set<GoodsReceiptNoteItem>();
    public DbSet<PurchaseBill> PurchaseBills => Set<PurchaseBill>();
    public DbSet<PurchaseBillItem> PurchaseBillItems => Set<PurchaseBillItem>();
    public DbSet<VendorPayment> VendorPayments => Set<VendorPayment>();

    // Phase 6 Accounting & General Ledger
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    public DbSet<JournalEntryLine> JournalEntryLines => Set<JournalEntryLine>();

    // Cloth Hub Retail Masters & Product Entities
    public DbSet<ClothBrand> ClothBrands => Set<ClothBrand>();
    public DbSet<ClothCategory> ClothCategories => Set<ClothCategory>();
    public DbSet<ClothSize> ClothSizes => Set<ClothSize>();
    public DbSet<ClothColour> ClothColours => Set<ClothColour>();
    public DbSet<ClothDesign> ClothDesigns => Set<ClothDesign>();
    public DbSet<ClothFabric> ClothFabrics => Set<ClothFabric>();
    public DbSet<ClothProduct> ClothProducts => Set<ClothProduct>();
    public DbSet<ClothProductVariant> ClothProductVariants => Set<ClothProductVariant>();

    // Cloth Hub Phase 3: Inventory & Box/Pack Assortments
    public DbSet<ClothBoxPack> ClothBoxPacks => Set<ClothBoxPack>();
    public DbSet<ClothBoxPackItem> ClothBoxPackItems => Set<ClothBoxPackItem>();
    public DbSet<ClothStockAdjustment> ClothStockAdjustments => Set<ClothStockAdjustment>();
    public DbSet<ClothStockAdjustmentItem> ClothStockAdjustmentItems => Set<ClothStockAdjustmentItem>();
    public DbSet<ClothStockLedger> ClothStockLedgers => Set<ClothStockLedger>();

    // Cloth Hub Phase 4: Apparel Procurement & Supplier Payables
    public DbSet<ClothSupplier> ClothSuppliers => Set<ClothSupplier>();
    public DbSet<ClothPurchaseBill> ClothPurchaseBills => Set<ClothPurchaseBill>();
    public DbSet<ClothPurchaseBillItem> ClothPurchaseBillItems => Set<ClothPurchaseBillItem>();
    public DbSet<ClothPurchaseReturn> ClothPurchaseReturns => Set<ClothPurchaseReturn>();
    public DbSet<ClothPurchaseReturnItem> ClothPurchaseReturnItems => Set<ClothPurchaseReturnItem>();

    // Cloth Hub Phase 5, 6 & 7: Retail POS Sales, Invoices, Returns, Customers & Expenses
    public DbSet<ClothSalesInvoice> ClothSalesInvoices => Set<ClothSalesInvoice>();
    public DbSet<ClothSalesInvoiceItem> ClothSalesInvoiceItems => Set<ClothSalesInvoiceItem>();
    public DbSet<ClothSalesReturn> ClothSalesReturns => Set<ClothSalesReturn>();
    public DbSet<ClothSalesReturnItem> ClothSalesReturnItems => Set<ClothSalesReturnItem>();
    public DbSet<ClothCustomer> ClothCustomers => Set<ClothCustomer>();
    public DbSet<ClothExpense> ClothExpenses => Set<ClothExpense>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Default decimal precision convention across all entities (avoids EF warnings & silent truncation)
        foreach (var property in modelBuilder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            if (property.GetColumnType() == null)
            {
                property.SetColumnType("decimal(18,2)");
            }
        }

        // Global Query Filters for Soft-Delete & Tenant Isolation
        modelBuilder.Entity<Business>().HasQueryFilter(b => !b.IsDeleted);

        modelBuilder.Entity<User>().HasQueryFilter(u => 
            !u.IsDeleted && 
            (_currentUserService.IsSuperAdmin || _currentUserService.BusinessId == null || u.BusinessId == _currentUserService.BusinessId));

        modelBuilder.Entity<Role>().HasQueryFilter(r => 
            !r.IsDeleted && 
            (_currentUserService.IsSuperAdmin || _currentUserService.BusinessId == null || r.BusinessId == null || r.BusinessId == _currentUserService.BusinessId));

        modelBuilder.Entity<Category>().HasQueryFilter(c => 
            !c.IsDeleted && 
            (_currentUserService.IsSuperAdmin || _currentUserService.BusinessId == null || c.BusinessId == _currentUserService.BusinessId));

        modelBuilder.Entity<UnitOfMeasure>().HasQueryFilter(u => 
            !u.IsDeleted && 
            (_currentUserService.IsSuperAdmin || _currentUserService.BusinessId == null || u.BusinessId == _currentUserService.BusinessId));

        modelBuilder.Entity<Warehouse>().HasQueryFilter(w => 
            !w.IsDeleted && 
            (_currentUserService.IsSuperAdmin || _currentUserService.BusinessId == null || w.BusinessId == _currentUserService.BusinessId));

        modelBuilder.Entity<Product>().HasQueryFilter(p => 
            !p.IsDeleted && 
            (_currentUserService.IsSuperAdmin || _currentUserService.BusinessId == null || p.BusinessId == _currentUserService.BusinessId));

        modelBuilder.Entity<InventoryStock>().HasQueryFilter(s => 
            !s.IsDeleted && 
            (_currentUserService.IsSuperAdmin || _currentUserService.BusinessId == null || s.BusinessId == _currentUserService.BusinessId));

        modelBuilder.Entity<StockTransaction>().HasQueryFilter(t => 
            !t.IsDeleted && 
            (_currentUserService.IsSuperAdmin || _currentUserService.BusinessId == null || t.BusinessId == _currentUserService.BusinessId));

        // Phase 4 Filters
        modelBuilder.Entity<Customer>().HasQueryFilter(c => 
            !c.IsDeleted && 
            (_currentUserService.IsSuperAdmin || _currentUserService.BusinessId == null || c.BusinessId == _currentUserService.BusinessId));

        modelBuilder.Entity<SalesOrder>().HasQueryFilter(o => 
            !o.IsDeleted && 
            (_currentUserService.IsSuperAdmin || _currentUserService.BusinessId == null || o.BusinessId == _currentUserService.BusinessId));

        modelBuilder.Entity<SalesOrderItem>().HasQueryFilter(i => !i.IsDeleted);

        modelBuilder.Entity<SalesInvoice>().HasQueryFilter(inv => 
            !inv.IsDeleted && 
            (_currentUserService.IsSuperAdmin || _currentUserService.BusinessId == null || inv.BusinessId == _currentUserService.BusinessId));

        modelBuilder.Entity<SalesInvoiceItem>().HasQueryFilter(i => !i.IsDeleted);

        modelBuilder.Entity<SalesPayment>().HasQueryFilter(p => 
            !p.IsDeleted && 
            (_currentUserService.IsSuperAdmin || _currentUserService.BusinessId == null || p.BusinessId == _currentUserService.BusinessId));

        // Phase 5 Procurement Filters
        modelBuilder.Entity<Supplier>().HasQueryFilter(s => 
            !s.IsDeleted && 
            (_currentUserService.IsSuperAdmin || _currentUserService.BusinessId == null || s.BusinessId == _currentUserService.BusinessId));

        modelBuilder.Entity<PurchaseOrder>().HasQueryFilter(po => 
            !po.IsDeleted && 
            (_currentUserService.IsSuperAdmin || _currentUserService.BusinessId == null || po.BusinessId == _currentUserService.BusinessId));

        modelBuilder.Entity<PurchaseOrderItem>().HasQueryFilter(i => !i.IsDeleted);

        modelBuilder.Entity<GoodsReceiptNote>().HasQueryFilter(grn => 
            !grn.IsDeleted && 
            (_currentUserService.IsSuperAdmin || _currentUserService.BusinessId == null || grn.BusinessId == _currentUserService.BusinessId));

        modelBuilder.Entity<GoodsReceiptNoteItem>().HasQueryFilter(i => !i.IsDeleted);

        modelBuilder.Entity<PurchaseBill>().HasQueryFilter(b => 
            !b.IsDeleted && 
            (_currentUserService.IsSuperAdmin || _currentUserService.BusinessId == null || b.BusinessId == _currentUserService.BusinessId));

        modelBuilder.Entity<PurchaseBillItem>().HasQueryFilter(i => !i.IsDeleted);

        modelBuilder.Entity<VendorPayment>().HasQueryFilter(vp => 
            !vp.IsDeleted && 
            (_currentUserService.IsSuperAdmin || _currentUserService.BusinessId == null || vp.BusinessId == _currentUserService.BusinessId));

        // Phase 6 Accounting Filters
        modelBuilder.Entity<Account>().HasQueryFilter(a => 
            !a.IsDeleted && 
            (_currentUserService.IsSuperAdmin || _currentUserService.BusinessId == null || a.BusinessId == _currentUserService.BusinessId));

        modelBuilder.Entity<JournalEntry>().HasQueryFilter(je => 
            !je.IsDeleted && 
            (_currentUserService.IsSuperAdmin || _currentUserService.BusinessId == null || je.BusinessId == _currentUserService.BusinessId));

        modelBuilder.Entity<JournalEntryLine>().HasQueryFilter(i => !i.IsDeleted);

        // Cloth Hub Retail Query Filters (Tenant Isolation & Soft Delete)
        modelBuilder.Entity<ClothBrand>().HasQueryFilter(b => 
            !b.IsDeleted && 
            (_currentUserService.IsSuperAdmin || _currentUserService.BusinessId == null || b.BusinessId == _currentUserService.BusinessId));

        modelBuilder.Entity<ClothCategory>().HasQueryFilter(c => 
            !c.IsDeleted && 
            (_currentUserService.IsSuperAdmin || _currentUserService.BusinessId == null || c.BusinessId == _currentUserService.BusinessId));

        modelBuilder.Entity<ClothSize>().HasQueryFilter(s => 
            !s.IsDeleted && 
            (_currentUserService.IsSuperAdmin || _currentUserService.BusinessId == null || s.BusinessId == _currentUserService.BusinessId));

        modelBuilder.Entity<ClothColour>().HasQueryFilter(c => 
            !c.IsDeleted && 
            (_currentUserService.IsSuperAdmin || _currentUserService.BusinessId == null || c.BusinessId == _currentUserService.BusinessId));

        modelBuilder.Entity<ClothDesign>().HasQueryFilter(d => 
            !d.IsDeleted && 
            (_currentUserService.IsSuperAdmin || _currentUserService.BusinessId == null || d.BusinessId == _currentUserService.BusinessId));

        modelBuilder.Entity<ClothFabric>().HasQueryFilter(f => 
            !f.IsDeleted && 
            (_currentUserService.IsSuperAdmin || _currentUserService.BusinessId == null || f.BusinessId == _currentUserService.BusinessId));

        modelBuilder.Entity<ClothProduct>().HasQueryFilter(p => 
            !p.IsDeleted && 
            (_currentUserService.IsSuperAdmin || _currentUserService.BusinessId == null || p.BusinessId == _currentUserService.BusinessId));

        modelBuilder.Entity<ClothProductVariant>().HasQueryFilter(v => 
            !v.IsDeleted && 
            (_currentUserService.IsSuperAdmin || _currentUserService.BusinessId == null || v.BusinessId == _currentUserService.BusinessId));

        // Cloth Hub Inventory Filters
        modelBuilder.Entity<ClothBoxPack>().HasQueryFilter(bp => 
            !bp.IsDeleted && 
            (_currentUserService.IsSuperAdmin || _currentUserService.BusinessId == null || bp.BusinessId == _currentUserService.BusinessId));

        modelBuilder.Entity<ClothBoxPackItem>().HasQueryFilter(i => !i.IsDeleted);

        modelBuilder.Entity<ClothStockAdjustment>().HasQueryFilter(sa => 
            !sa.IsDeleted && 
            (_currentUserService.IsSuperAdmin || _currentUserService.BusinessId == null || sa.BusinessId == _currentUserService.BusinessId));

        modelBuilder.Entity<ClothStockAdjustmentItem>().HasQueryFilter(i => !i.IsDeleted);

        modelBuilder.Entity<ClothStockLedger>().HasQueryFilter(sl => 
            !sl.IsDeleted && 
            (_currentUserService.IsSuperAdmin || _currentUserService.BusinessId == null || sl.BusinessId == _currentUserService.BusinessId));

        // Cloth Hub Phase 4 Purchase Filters
        modelBuilder.Entity<ClothSupplier>().HasQueryFilter(s => 
            !s.IsDeleted && 
            (_currentUserService.IsSuperAdmin || _currentUserService.BusinessId == null || s.BusinessId == _currentUserService.BusinessId));

        modelBuilder.Entity<ClothPurchaseBill>().HasQueryFilter(pb => 
            !pb.IsDeleted && 
            (_currentUserService.IsSuperAdmin || _currentUserService.BusinessId == null || pb.BusinessId == _currentUserService.BusinessId));

        modelBuilder.Entity<ClothPurchaseBillItem>().HasQueryFilter(i => !i.IsDeleted);

        modelBuilder.Entity<ClothPurchaseReturn>().HasQueryFilter(pr => 
            !pr.IsDeleted && 
            (_currentUserService.IsSuperAdmin || _currentUserService.BusinessId == null || pr.BusinessId == _currentUserService.BusinessId));

        modelBuilder.Entity<ClothPurchaseReturnItem>().HasQueryFilter(i => !i.IsDeleted);

        // Cloth Hub Phase 5 Sales Filters
        modelBuilder.Entity<ClothSalesInvoice>().HasQueryFilter(si => 
            !si.IsDeleted && 
            (_currentUserService.IsSuperAdmin || _currentUserService.BusinessId == null || si.BusinessId == _currentUserService.BusinessId));

        modelBuilder.Entity<ClothSalesInvoiceItem>().HasQueryFilter(i => !i.IsDeleted);

        // Cloth Hub Phase 6 Exchanges & Returns Filters
        modelBuilder.Entity<ClothSalesReturn>().HasQueryFilter(sr => 
            !sr.IsDeleted && 
            (_currentUserService.IsSuperAdmin || _currentUserService.BusinessId == null || sr.BusinessId == _currentUserService.BusinessId));

        modelBuilder.Entity<ClothSalesReturnItem>().HasQueryFilter(i => !i.IsDeleted);

        // Cloth Hub Phase 7 Customer & Expense Filters
        modelBuilder.Entity<ClothCustomer>().HasQueryFilter(c => 
            !c.IsDeleted && 
            (_currentUserService.IsSuperAdmin || _currentUserService.BusinessId == null || c.BusinessId == _currentUserService.BusinessId));

        modelBuilder.Entity<ClothExpense>().HasQueryFilter(e => 
            !e.IsDeleted && 
            (_currentUserService.IsSuperAdmin || _currentUserService.BusinessId == null || e.BusinessId == _currentUserService.BusinessId));
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var userEmail = _currentUserService.Email ?? "System";
        var now = _dateTimeProvider.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedOn = now;
                    entry.Entity.CreatedBy = userEmail;
                    entry.Entity.IsDeleted = false;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedOn = now;
                    entry.Entity.UpdatedBy = userEmail;
                    break;

                case EntityState.Deleted:
                    // Soft-delete intercept
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.UpdatedOn = now;
                    entry.Entity.UpdatedBy = userEmail;
                    break;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
