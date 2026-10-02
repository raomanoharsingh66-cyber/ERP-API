using BizFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace BizFlow.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Business> Businesses { get; }
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<AuditLog> AuditLogs { get; }

    // Phase 3 Inventory & Merchandise
    DbSet<Category> Categories { get; }
    DbSet<UnitOfMeasure> UnitsOfMeasure { get; }
    DbSet<Warehouse> Warehouses { get; }
    DbSet<Product> Products { get; }
    DbSet<InventoryStock> InventoryStocks { get; }
    DbSet<StockTransaction> StockTransactions { get; }

    // Phase 4 Commercial & Sales
    DbSet<Customer> Customers { get; }
    DbSet<SalesOrder> SalesOrders { get; }
    DbSet<SalesOrderItem> SalesOrderItems { get; }
    DbSet<SalesInvoice> SalesInvoices { get; }
    DbSet<SalesInvoiceItem> SalesInvoiceItems { get; }
    DbSet<SalesPayment> SalesPayments { get; }

    // Phase 5 Procurement & Vendor Payables
    DbSet<Supplier> Suppliers { get; }
    DbSet<PurchaseOrder> PurchaseOrders { get; }
    DbSet<PurchaseOrderItem> PurchaseOrderItems { get; }
    DbSet<GoodsReceiptNote> GoodsReceiptNotes { get; }
    DbSet<GoodsReceiptNoteItem> GoodsReceiptNoteItems { get; }
    DbSet<PurchaseBill> PurchaseBills { get; }
    DbSet<PurchaseBillItem> PurchaseBillItems { get; }
    DbSet<VendorPayment> VendorPayments { get; }

    // Phase 6 Accounting & General Ledger
    DbSet<Account> Accounts { get; }
    DbSet<JournalEntry> JournalEntries { get; }
    DbSet<JournalEntryLine> JournalEntryLines { get; }

    // Cloth Hub Masters & Products
    DbSet<BizFlow.Domain.Entities.ClothHub.ClothBrand> ClothBrands { get; }
    DbSet<BizFlow.Domain.Entities.ClothHub.ClothCategory> ClothCategories { get; }
    DbSet<BizFlow.Domain.Entities.ClothHub.ClothSize> ClothSizes { get; }
    DbSet<BizFlow.Domain.Entities.ClothHub.ClothColour> ClothColours { get; }
    DbSet<BizFlow.Domain.Entities.ClothHub.ClothDesign> ClothDesigns { get; }
    DbSet<BizFlow.Domain.Entities.ClothHub.ClothFabric> ClothFabrics { get; }
    DbSet<BizFlow.Domain.Entities.ClothHub.ClothProduct> ClothProducts { get; }
    DbSet<BizFlow.Domain.Entities.ClothHub.ClothProductVariant> ClothProductVariants { get; }

    // Cloth Hub Phase 3: Inventory & Box/Pack Assortments
    DbSet<BizFlow.Domain.Entities.ClothHub.ClothBoxPack> ClothBoxPacks { get; }
    DbSet<BizFlow.Domain.Entities.ClothHub.ClothBoxPackItem> ClothBoxPackItems { get; }
    DbSet<BizFlow.Domain.Entities.ClothHub.ClothStockAdjustment> ClothStockAdjustments { get; }
    DbSet<BizFlow.Domain.Entities.ClothHub.ClothStockAdjustmentItem> ClothStockAdjustmentItems { get; }
    DbSet<BizFlow.Domain.Entities.ClothHub.ClothStockLedger> ClothStockLedgers { get; }

    // Cloth Hub Phase 4: Apparel Procurement & Supplier Payables
    DbSet<BizFlow.Domain.Entities.ClothHub.ClothSupplier> ClothSuppliers { get; }
    DbSet<BizFlow.Domain.Entities.ClothHub.ClothPurchaseBill> ClothPurchaseBills { get; }
    DbSet<BizFlow.Domain.Entities.ClothHub.ClothPurchaseBillItem> ClothPurchaseBillItems { get; }
    DbSet<BizFlow.Domain.Entities.ClothHub.ClothPurchaseReturn> ClothPurchaseReturns { get; }
    DbSet<BizFlow.Domain.Entities.ClothHub.ClothPurchaseReturnItem> ClothPurchaseReturnItems { get; }

    // Cloth Hub Phase 5, 6 & 7: Sales, Returns, Customers & Expenses
    DbSet<BizFlow.Domain.Entities.ClothHub.ClothSalesInvoice> ClothSalesInvoices { get; }
    DbSet<BizFlow.Domain.Entities.ClothHub.ClothSalesInvoiceItem> ClothSalesInvoiceItems { get; }
    DbSet<BizFlow.Domain.Entities.ClothHub.ClothSalesReturn> ClothSalesReturns { get; }
    DbSet<BizFlow.Domain.Entities.ClothHub.ClothSalesReturnItem> ClothSalesReturnItems { get; }
    DbSet<BizFlow.Domain.Entities.ClothHub.ClothCustomer> ClothCustomers { get; }
    DbSet<BizFlow.Domain.Entities.ClothHub.ClothExpense> ClothExpenses { get; }

    DatabaseFacade Database { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
