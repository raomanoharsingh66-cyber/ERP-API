using BizFlow.Application.Common.Interfaces;
using BizFlow.Domain.Constants;
using BizFlow.Domain.Entities;
using BizFlow.Domain.Entities.ClothHub;
using BizFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BizFlow.Infrastructure.Data;

public class DatabaseSeeder : IDatabaseSeeder
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IDateTimeProvider dateTimeProvider,
        ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Seed Permissions Catalog
            var existingPermissions = await _context.Permissions
                .ToDictionaryAsync(p => p.Code, p => p, cancellationToken);

            var allDefinitions = Permissions.GetAll();
            var addedPermissions = false;

            foreach (var def in allDefinitions)
            {
                if (!existingPermissions.ContainsKey(def.Code))
                {
                    _context.Permissions.Add(new Permission
                    {
                        Id = Guid.NewGuid(),
                        Code = def.Code,
                        Module = def.Module,
                        Description = def.Description
                    });
                    addedPermissions = true;
                }
            }

            if (addedPermissions)
            {
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Seeded permissions catalog.");
            }

            // Reload all permissions
            var allPermissions = await _context.Permissions.ToListAsync(cancellationToken);

            // Ensure all existing BusinessAdmin roles have all permissions (including ClothHub permissions)
            var businessAdminRoles = await _context.Roles
                .IgnoreQueryFilters()
                .Where(r => r.Name == "BusinessAdmin")
                .ToListAsync(cancellationToken);

            foreach (var role in businessAdminRoles)
            {
                var rolePermIds = await _context.RolePermissions
                    .Where(rp => rp.RoleId == role.Id)
                    .Select(rp => rp.PermissionId)
                    .ToListAsync(cancellationToken);

                var missingPerms = allPermissions.Where(p => !rolePermIds.Contains(p.Id)).ToList();
                if (missingPerms.Count > 0)
                {
                    foreach (var missing in missingPerms)
                    {
                        _context.RolePermissions.Add(new RolePermission
                        {
                            RoleId = role.Id,
                            PermissionId = missing.Id
                        });
                    }
                    await _context.SaveChangesAsync(cancellationToken);
                }
            }

            // 2. Seed SuperAdmin Role & SuperAdmin User
            var superAdminRole = await _context.Roles
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(r => r.Name == "SuperAdmin" && r.BusinessId == null, cancellationToken);

            if (superAdminRole == null)
            {
                superAdminRole = new Role
                {
                    Id = Guid.NewGuid(),
                    BusinessId = null,
                    Name = "SuperAdmin",
                    Description = "Global platform administrator with unrestricted access.",
                    IsSystemRole = true,
                    IsActive = true,
                    CreatedOn = _dateTimeProvider.UtcNow,
                    CreatedBy = "System"
                };

                _context.Roles.Add(superAdminRole);
                await _context.SaveChangesAsync(cancellationToken);
            }

            var superAdminUser = await _context.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Email == "admin@bizflow.local", cancellationToken);

            if (superAdminUser == null)
            {
                superAdminUser = new User
                {
                    Id = Guid.NewGuid(),
                    BusinessId = null,
                    FirstName = "BizFlow",
                    LastName = "Administrator",
                    Email = "admin@bizflow.local",
                    PasswordHash = _passwordHasher.HashPassword("AdminPassword@2026!"),
                    PhoneNumber = "+919876543210",
                    IsActive = true,
                    CreatedOn = _dateTimeProvider.UtcNow,
                    CreatedBy = "System"
                };

                _context.Users.Add(superAdminUser);

                _context.UserRoles.Add(new UserRole
                {
                    UserId = superAdminUser.Id,
                    RoleId = superAdminRole.Id
                });

                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Seeded SuperAdmin user: admin@bizflow.local");
            }

            // 3. Seed Default Demo Business (Acme Global Ltd)
            var demoBusiness = await _context.Businesses
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(b => b.BusinessCode == "ACME-01", cancellationToken);

            if (demoBusiness == null)
            {
                demoBusiness = new Business
                {
                    Id = Guid.NewGuid(),
                    BusinessCode = "ACME-01",
                    Name = "Acme Global Ltd",
                    LegalName = "Acme Global Private Limited",
                    GSTNumber = "27AAACA1234A1Z5",
                    Email = "info@acmeglobal.com",
                    Phone = "+912288776655",
                    Address = "Tower 4, Bandra Kurla Complex, Mumbai, Maharashtra 400051",
                    Currency = "INR",
                    IsActive = true,
                    CreatedOn = _dateTimeProvider.UtcNow,
                    CreatedBy = "System"
                };

                _context.Businesses.Add(demoBusiness);

                if (superAdminUser.BusinessId == null)
                {
                    superAdminUser.BusinessId = demoBusiness.Id;
                }

                var businessAdminRole = new Role
                {
                    Id = Guid.NewGuid(),
                    BusinessId = demoBusiness.Id,
                    Name = "BusinessAdmin",
                    Description = "Tenant Business Administrator",
                    IsSystemRole = false,
                    IsActive = true,
                    CreatedOn = _dateTimeProvider.UtcNow,
                    CreatedBy = "System"
                };

                _context.Roles.Add(businessAdminRole);

                foreach (var p in allPermissions)
                {
                    _context.RolePermissions.Add(new RolePermission
                    {
                        RoleId = businessAdminRole.Id,
                        PermissionId = p.Id
                    });
                }

                var tenantUser = new User
                {
                    Id = Guid.NewGuid(),
                    BusinessId = demoBusiness.Id,
                    FirstName = "Manohar",
                    LastName = "Singh",
                    Email = "manohar@acmeglobal.com",
                    PasswordHash = _passwordHasher.HashPassword("AdminPassword@2026!"),
                    PhoneNumber = "+919876543211",
                    IsActive = true,
                    CreatedOn = _dateTimeProvider.UtcNow,
                    CreatedBy = "System"
                };

                _context.Users.Add(tenantUser);

                _context.UserRoles.Add(new UserRole
                {
                    UserId = tenantUser.Id,
                    RoleId = businessAdminRole.Id
                });

                // 4. Seed Inventory: UOMs, Categories, Warehouse, Products
                var uomPcs = new UnitOfMeasure { Id = Guid.NewGuid(), BusinessId = demoBusiness.Id, Name = "Piece", Code = "PCS", CreatedOn = _dateTimeProvider.UtcNow, CreatedBy = "System" };
                var uomKg = new UnitOfMeasure { Id = Guid.NewGuid(), BusinessId = demoBusiness.Id, Name = "Kilogram", Code = "KG", CreatedOn = _dateTimeProvider.UtcNow, CreatedBy = "System" };
                var uomBox = new UnitOfMeasure { Id = Guid.NewGuid(), BusinessId = demoBusiness.Id, Name = "Box", Code = "BOX", CreatedOn = _dateTimeProvider.UtcNow, CreatedBy = "System" };

                _context.UnitsOfMeasure.AddRange(uomPcs, uomKg, uomBox);

                var catFinished = new Category { Id = Guid.NewGuid(), BusinessId = demoBusiness.Id, Name = "Finished Goods", Code = "CAT-FG", Description = "Ready-to-sell manufactured goods", CreatedOn = _dateTimeProvider.UtcNow, CreatedBy = "System" };
                var catRaw = new Category { Id = Guid.NewGuid(), BusinessId = demoBusiness.Id, Name = "Raw Materials", Code = "CAT-RAW", Description = "Procured parts and inputs", CreatedOn = _dateTimeProvider.UtcNow, CreatedBy = "System" };

                _context.Categories.AddRange(catFinished, catRaw);

                var warehouse = new Warehouse
                {
                    Id = Guid.NewGuid(),
                    BusinessId = demoBusiness.Id,
                    Name = "Central Warehouse (Mumbai Hub)",
                    Code = "WH-01",
                    Address = "Bldg C, Logistics Park, Bhiwandi, Maharashtra",
                    ContactPerson = "Ramesh Patil",
                    Phone = "+919820011223",
                    IsDefault = true,
                    IsActive = true,
                    CreatedOn = _dateTimeProvider.UtcNow,
                    CreatedBy = "System"
                };

                _context.Warehouses.Add(warehouse);

                var p1 = new Product
                {
                    Id = Guid.NewGuid(),
                    BusinessId = demoBusiness.Id,
                    CategoryId = catFinished.Id,
                    UnitOfMeasureId = uomPcs.Id,
                    SKU = "PROD-STEEL-01",
                    Barcode = "8901234567890",
                    Name = "Industrial Steel Bearings 6205",
                    Description = "High precision deep groove steel ball bearings",
                    PurchasePrice = 320.00m,
                    SellingPrice = 480.00m,
                    TaxRate = 18.00m,
                    HSNCode = "84821011",
                    MinStockLevel = 25m,
                    MaxStockLevel = 500m,
                    IsActive = true,
                    CreatedOn = _dateTimeProvider.UtcNow,
                    CreatedBy = "System"
                };

                var p2 = new Product
                {
                    Id = Guid.NewGuid(),
                    BusinessId = demoBusiness.Id,
                    CategoryId = catFinished.Id,
                    UnitOfMeasureId = uomBox.Id,
                    SKU = "PROD-VALVE-02",
                    Barcode = "8901234567891",
                    Name = "Brass Gate Valve 2-Inch",
                    Description = "Heavy duty water and steam pipeline control valve",
                    PurchasePrice = 1250.00m,
                    SellingPrice = 1800.00m,
                    TaxRate = 18.00m,
                    HSNCode = "84818030",
                    MinStockLevel = 10m,
                    MaxStockLevel = 200m,
                    IsActive = true,
                    CreatedOn = _dateTimeProvider.UtcNow,
                    CreatedBy = "System"
                };

                var p3 = new Product
                {
                    Id = Guid.NewGuid(),
                    BusinessId = demoBusiness.Id,
                    CategoryId = catRaw.Id,
                    UnitOfMeasureId = uomKg.Id,
                    SKU = "RAW-ALUM-ROD",
                    Barcode = "8901234567892",
                    Name = "Aluminum Alloy Extrusion Rod 6061",
                    Description = "Structural aerospace grade aluminum bar stock",
                    PurchasePrice = 280.00m,
                    SellingPrice = 360.00m,
                    TaxRate = 18.00m,
                    HSNCode = "76042910",
                    MinStockLevel = 100m,
                    MaxStockLevel = 2000m,
                    IsActive = true,
                    CreatedOn = _dateTimeProvider.UtcNow,
                    CreatedBy = "System"
                };

                _context.Products.AddRange(p1, p2, p3);

                // Initial Stocks
                var stock1 = new InventoryStock { Id = Guid.NewGuid(), BusinessId = demoBusiness.Id, ProductId = p1.Id, WarehouseId = warehouse.Id, QuantityOnHand = 120m, QuantityReserved = 15m, AverageCost = 320.00m, CreatedOn = _dateTimeProvider.UtcNow, CreatedBy = "System" };
                var stock2 = new InventoryStock { Id = Guid.NewGuid(), BusinessId = demoBusiness.Id, ProductId = p2.Id, WarehouseId = warehouse.Id, QuantityOnHand = 8m, QuantityReserved = 0m, AverageCost = 1250.00m, CreatedOn = _dateTimeProvider.UtcNow, CreatedBy = "System" }; // Low stock demo!
                var stock3 = new InventoryStock { Id = Guid.NewGuid(), BusinessId = demoBusiness.Id, ProductId = p3.Id, WarehouseId = warehouse.Id, QuantityOnHand = 450m, QuantityReserved = 50m, AverageCost = 280.00m, CreatedOn = _dateTimeProvider.UtcNow, CreatedBy = "System" };

                _context.InventoryStocks.AddRange(stock1, stock2, stock3);

                _context.StockTransactions.Add(new StockTransaction
                {
                    Id = Guid.NewGuid(),
                    BusinessId = demoBusiness.Id,
                    ProductId = p1.Id,
                    WarehouseId = warehouse.Id,
                    TransactionType = StockTransactionType.Inward,
                    Quantity = 120m,
                    UnitPrice = 320.00m,
                    TotalAmount = 120m * 320.00m,
                    ReferenceType = "InitialStock",
                    ReferenceId = "SYS-INIT",
                    Notes = "Initial stock allocation",
                    Timestamp = _dateTimeProvider.UtcNow,
                    CreatedOn = _dateTimeProvider.UtcNow,
                    CreatedBy = "System"
                });

                // Phase 4 Commercial & Sales Seed Data
                var cust1 = new Customer
                {
                    Id = Guid.NewGuid(),
                    BusinessId = demoBusiness.Id,
                    CustomerCode = "CUST-2026-0001",
                    Name = "Apex Industrial Supplies Pvt Ltd",
                    Email = "procurement@apexindustrial.in",
                    Phone = "+919820055443",
                    GSTIN = "27AAACA1234A1Z5",
                    PAN = "AAACA1234A",
                    BillingAddress = "Plot 42, MIDC Industrial Area, Andheri East",
                    BillingCity = "Mumbai",
                    BillingState = "Maharashtra",
                    BillingPostalCode = "400093",
                    BillingCountry = "India",
                    ShippingAddress = "Plot 42, MIDC Industrial Area, Andheri East",
                    ShippingCity = "Mumbai",
                    ShippingState = "Maharashtra",
                    ShippingPostalCode = "400093",
                    ShippingCountry = "India",
                    CreditLimit = 500000.00m,
                    OutstandingBalance = 11328.00m,
                    IsActive = true,
                    Notes = "Tier 1 manufacturing client, 30 days net terms",
                    CreatedOn = _dateTimeProvider.UtcNow,
                    CreatedBy = "System"
                };

                var cust2 = new Customer
                {
                    Id = Guid.NewGuid(),
                    BusinessId = demoBusiness.Id,
                    CustomerCode = "CUST-2026-0002",
                    Name = "Zenith Technologies Inc",
                    Email = "accounts@zenithtech.org",
                    Phone = "+918041239988",
                    GSTIN = "29AAACZ9876B1Z2",
                    PAN = "AAACZ9876B",
                    BillingAddress = "Tower 3, Electronic City Phase 1",
                    BillingCity = "Bengaluru",
                    BillingState = "Karnataka",
                    BillingPostalCode = "560100",
                    BillingCountry = "India",
                    ShippingAddress = "Tower 3, Electronic City Phase 1",
                    ShippingCity = "Bengaluru",
                    ShippingState = "Karnataka",
                    ShippingPostalCode = "560100",
                    ShippingCountry = "India",
                    CreditLimit = 250000.00m,
                    OutstandingBalance = 0m,
                    IsActive = true,
                    Notes = "Interstate IT infrastructure customer",
                    CreatedOn = _dateTimeProvider.UtcNow,
                    CreatedBy = "System"
                };

                _context.Customers.AddRange(cust1, cust2);

                // Sample Sales Order
                var sampleOrder = new SalesOrder
                {
                    Id = Guid.NewGuid(),
                    BusinessId = demoBusiness.Id,
                    OrderNumber = "SO-2026-0001",
                    OrderDate = _dateTimeProvider.UtcNow.UtcDateTime.AddDays(-5),
                    ExpectedDeliveryDate = _dateTimeProvider.UtcNow.UtcDateTime.AddDays(2),
                    CustomerId = cust1.Id,
                    WarehouseId = warehouse.Id,
                    Status = OrderStatus.Confirmed,
                    SubTotal = 9600.00m,
                    DiscountAmount = 0m,
                    TaxAmount = 1728.00m,
                    TotalAmount = 11328.00m,
                    Notes = "Urgent delivery for upcoming plant maintenance schedule",
                    CreatedOn = _dateTimeProvider.UtcNow.AddDays(-5),
                    CreatedBy = "System"
                };

                sampleOrder.Items.Add(new SalesOrderItem
                {
                    Id = Guid.NewGuid(),
                    SalesOrderId = sampleOrder.Id,
                    ProductId = p1.Id,
                    Quantity = 20m,
                    UnitPrice = 480.00m,
                    DiscountPercentage = 0m,
                    DiscountAmount = 0m,
                    TaxRate = 18.00m,
                    TaxAmount = 1728.00m,
                    TotalAmount = 11328.00m,
                    Notes = "High precision batch"
                });

                _context.SalesOrders.Add(sampleOrder);

                // Sample Sales Invoice
                var sampleInvoice = new SalesInvoice
                {
                    Id = Guid.NewGuid(),
                    BusinessId = demoBusiness.Id,
                    InvoiceNumber = "INV-2026-0001",
                    InvoiceDate = _dateTimeProvider.UtcNow.UtcDateTime.AddDays(-3),
                    DueDate = _dateTimeProvider.UtcNow.UtcDateTime.AddDays(27),
                    SalesOrderId = sampleOrder.Id,
                    CustomerId = cust1.Id,
                    WarehouseId = warehouse.Id,
                    Status = InvoiceStatus.PartiallyPaid,
                    SubTotal = 9600.00m,
                    DiscountAmount = 0m,
                    TaxAmount = 1728.00m,
                    CgstAmount = 864.00m,
                    SgstAmount = 864.00m,
                    IgstAmount = 0m,
                    TotalAmount = 11328.00m,
                    PaidAmount = 5000.00m,
                    BalanceAmount = 6328.00m,
                    Notes = "Tax Invoice for purchase order PO-MIDC-9921",
                    CreatedOn = _dateTimeProvider.UtcNow.AddDays(-3),
                    CreatedBy = "System"
                };

                sampleInvoice.Items.Add(new SalesInvoiceItem
                {
                    Id = Guid.NewGuid(),
                    SalesInvoiceId = sampleInvoice.Id,
                    ProductId = p1.Id,
                    Quantity = 20m,
                    UnitPrice = 480.00m,
                    DiscountPercentage = 0m,
                    DiscountAmount = 0m,
                    TaxRate = 18.00m,
                    TaxAmount = 1728.00m,
                    TotalAmount = 11328.00m,
                    Notes = "Steel Bearings 6205"
                });

                // Deduct stock for seeded invoice
                stock1.QuantityOnHand -= 20m;
                _context.StockTransactions.Add(new StockTransaction
                {
                    Id = Guid.NewGuid(),
                    BusinessId = demoBusiness.Id,
                    ProductId = p1.Id,
                    WarehouseId = warehouse.Id,
                    TransactionType = StockTransactionType.Outward,
                    Quantity = 20m,
                    UnitPrice = 320.00m,
                    TotalAmount = 20m * 320.00m,
                    ReferenceType = "SalesInvoice",
                    ReferenceId = sampleInvoice.InvoiceNumber,
                    Notes = $"Billed to {cust1.Name}",
                    Timestamp = _dateTimeProvider.UtcNow.AddDays(-3),
                    CreatedOn = _dateTimeProvider.UtcNow.AddDays(-3),
                    CreatedBy = "System"
                });

                var samplePayment = new SalesPayment
                {
                    Id = Guid.NewGuid(),
                    BusinessId = demoBusiness.Id,
                    PaymentNumber = "PAY-2026-0001",
                    SalesInvoiceId = sampleInvoice.Id,
                    CustomerId = cust1.Id,
                    PaymentDate = _dateTimeProvider.UtcNow.UtcDateTime.AddDays(-1),
                    Amount = 5000.00m,
                    Method = PaymentMethod.BankTransfer,
                    ReferenceNumber = "HDFC-NEFT-987211",
                    Notes = "Advance part payment received",
                    CreatedOn = _dateTimeProvider.UtcNow.AddDays(-1),
                    CreatedBy = "System"
                };

                _context.SalesInvoices.Add(sampleInvoice);
                _context.SalesPayments.Add(samplePayment);

                // Phase 5 Procurement & Vendor Seed Data
                var supp1 = new Supplier
                {
                    Id = Guid.NewGuid(),
                    BusinessId = demoBusiness.Id,
                    SupplierCode = "SUPP-2026-0001",
                    Name = "Bharat Heavy Metals Corp",
                    ContactPerson = "Rajesh Sharma",
                    Email = "sales@bharatmetals.in",
                    Phone = "+912027118899",
                    GSTIN = "27AAACB5678C1Z8",
                    PAN = "AAACB5678C",
                    BillingAddress = "Sector 10, Bhosari Industrial Area",
                    City = "Pune",
                    State = "Maharashtra",
                    PostalCode = "411026",
                    Country = "India",
                    PaymentTermsDays = 30,
                    OutstandingPayable = 16875.00m,
                    IsActive = true,
                    Notes = "Strategic raw material and castings partner",
                    CreatedOn = _dateTimeProvider.UtcNow,
                    CreatedBy = "System"
                };

                var supp2 = new Supplier
                {
                    Id = Guid.NewGuid(),
                    BusinessId = demoBusiness.Id,
                    SupplierCode = "SUPP-2026-0002",
                    Name = "Techno Valves & Fittings Ltd",
                    ContactPerson = "Anil Patel",
                    Email = "orders@technovalves.com",
                    Phone = "+917926554433",
                    GSTIN = "24AAACT4321D1Z1",
                    PAN = "AAACT4321D",
                    BillingAddress = "GIDC Estate, Vatva",
                    City = "Ahmedabad",
                    State = "Gujarat",
                    PostalCode = "382445",
                    Country = "India",
                    PaymentTermsDays = 45,
                    OutstandingPayable = 0m,
                    IsActive = true,
                    Notes = "Precision engineering valve supplier (Interstate)",
                    CreatedOn = _dateTimeProvider.UtcNow,
                    CreatedBy = "System"
                };

                _context.Suppliers.AddRange(supp1, supp2);

                // Sample Purchase Order
                var samplePO = new PurchaseOrder
                {
                    Id = Guid.NewGuid(),
                    BusinessId = demoBusiness.Id,
                    PONumber = "PO-2026-0001",
                    OrderDate = _dateTimeProvider.UtcNow.UtcDateTime.AddDays(-7),
                    ExpectedDeliveryDate = _dateTimeProvider.UtcNow.UtcDateTime.AddDays(7),
                    SupplierId = supp1.Id,
                    WarehouseId = warehouse.Id,
                    Status = PurchaseOrderStatus.PartiallyReceived,
                    SubTotal = 31250.00m,
                    TaxAmount = 5625.00m,
                    TotalAmount = 36875.00m,
                    Notes = "Procurement of Brass Gate Valves for plant assembly",
                    CreatedOn = _dateTimeProvider.UtcNow.AddDays(-7),
                    CreatedBy = "System"
                };

                var poLine1 = new PurchaseOrderItem
                {
                    Id = Guid.NewGuid(),
                    PurchaseOrderId = samplePO.Id,
                    ProductId = p2.Id,
                    OrderedQuantity = 25m,
                    ReceivedQuantity = 15m, // 10 still pending
                    UnitPrice = 1250.00m,
                    TaxRate = 18.00m,
                    TaxAmount = 5625.00m,
                    TotalAmount = 36875.00m,
                    Notes = "Heavy duty batch"
                };

                samplePO.Items.Add(poLine1);
                _context.PurchaseOrders.Add(samplePO);

                // Sample Goods Receipt Note (GRN)
                var sampleGRN = new GoodsReceiptNote
                {
                    Id = Guid.NewGuid(),
                    BusinessId = demoBusiness.Id,
                    GRNNumber = "GRN-2026-0001",
                    ReceiptDate = _dateTimeProvider.UtcNow.UtcDateTime.AddDays(-4),
                    PurchaseOrderId = samplePO.Id,
                    SupplierId = supp1.Id,
                    WarehouseId = warehouse.Id,
                    SupplierDeliveryNoteNo = "DC-BHM-9821",
                    Status = GRNStatus.Verified,
                    Notes = "First consignment received in good condition",
                    CreatedOn = _dateTimeProvider.UtcNow.AddDays(-4),
                    CreatedBy = "System"
                };

                sampleGRN.Items.Add(new GoodsReceiptNoteItem
                {
                    Id = Guid.NewGuid(),
                    GoodsReceiptNoteId = sampleGRN.Id,
                    ProductId = p2.Id,
                    PurchaseOrderItemId = poLine1.Id,
                    ReceivedQuantity = 15m,
                    AcceptedQuantity = 15m,
                    RejectedQuantity = 0m,
                    UnitPrice = 1250.00m,
                    Notes = "Quality inspection passed"
                });

                // Replenish stock for received GRN
                stock2.QuantityOnHand += 15m;
                _context.StockTransactions.Add(new StockTransaction
                {
                    Id = Guid.NewGuid(),
                    BusinessId = demoBusiness.Id,
                    ProductId = p2.Id,
                    WarehouseId = warehouse.Id,
                    TransactionType = StockTransactionType.Inward,
                    Quantity = 15m,
                    UnitPrice = 1250.00m,
                    TotalAmount = 15m * 1250.00m,
                    ReferenceType = "GoodsReceiptNote",
                    ReferenceId = sampleGRN.GRNNumber,
                    Notes = $"Received from {supp1.Name}. Challan: DC-BHM-9821",
                    Timestamp = _dateTimeProvider.UtcNow.AddDays(-4),
                    CreatedOn = _dateTimeProvider.UtcNow.AddDays(-4),
                    CreatedBy = "System"
                });

                _context.GoodsReceiptNotes.Add(sampleGRN);

                // Sample Purchase Bill (Vendor Invoice)
                var sampleBill = new PurchaseBill
                {
                    Id = Guid.NewGuid(),
                    BusinessId = demoBusiness.Id,
                    BillNumber = "BILL-2026-0001",
                    VendorInvoiceNumber = "INV-BHM-2026-881",
                    BillDate = _dateTimeProvider.UtcNow.UtcDateTime.AddDays(-3),
                    DueDate = _dateTimeProvider.UtcNow.UtcDateTime.AddDays(27),
                    PurchaseOrderId = samplePO.Id,
                    SupplierId = supp1.Id,
                    WarehouseId = warehouse.Id,
                    Status = BillStatus.PartiallyPaid,
                    SubTotal = 31250.00m,
                    TaxAmount = 5625.00m,
                    CgstAmount = 2812.50m,
                    SgstAmount = 2812.50m,
                    IgstAmount = 0m,
                    TotalAmount = 36875.00m,
                    PaidAmount = 20000.00m,
                    BalanceAmount = 16875.00m,
                    Notes = "Bill corresponding to PO-2026-0001 and GRN-2026-0001",
                    CreatedOn = _dateTimeProvider.UtcNow.AddDays(-3),
                    CreatedBy = "System"
                };

                sampleBill.Items.Add(new PurchaseBillItem
                {
                    Id = Guid.NewGuid(),
                    PurchaseBillId = sampleBill.Id,
                    ProductId = p2.Id,
                    Quantity = 25m,
                    UnitPrice = 1250.00m,
                    TaxRate = 18.00m,
                    TaxAmount = 5625.00m,
                    TotalAmount = 36875.00m,
                    Notes = "Brass Gate Valve 2-Inch"
                });

                var sampleDisbursement = new VendorPayment
                {
                    Id = Guid.NewGuid(),
                    BusinessId = demoBusiness.Id,
                    PaymentNumber = "VPAY-2026-0001",
                    PurchaseBillId = sampleBill.Id,
                    SupplierId = supp1.Id,
                    PaymentDate = _dateTimeProvider.UtcNow.UtcDateTime.AddDays(-1),
                    Amount = 20000.00m,
                    Method = PaymentMethod.BankTransfer,
                    ReferenceNumber = "ICICI-RTGS-554210",
                    Notes = "Advance part payment disbursed",
                    CreatedOn = _dateTimeProvider.UtcNow.AddDays(-1),
                    CreatedBy = "System"
                };

                _context.PurchaseBills.Add(sampleBill);
                _context.VendorPayments.Add(sampleDisbursement);

                // Phase 6 Standard Chart of Accounts & General Ledger
                if (!await _context.Accounts.IgnoreQueryFilters().AnyAsync(a => a.BusinessId == demoBusiness.Id, cancellationToken))
                {
                    var accBank = new Account { Id = Guid.NewGuid(), BusinessId = demoBusiness.Id, AccountCode = "1020", AccountName = "HDFC Current Bank Account", Type = AccountType.Asset, Subtype = "Bank", CurrentBalance = 1500000m, IsSystemAccount = true, IsActive = true, CreatedBy = "System", CreatedOn = _dateTimeProvider.UtcNow };
                    var accCash = new Account { Id = Guid.NewGuid(), BusinessId = demoBusiness.Id, AccountCode = "1010", AccountName = "Cash on Hand", Type = AccountType.Asset, Subtype = "Cash", CurrentBalance = 50000m, IsSystemAccount = true, IsActive = true, CreatedBy = "System", CreatedOn = _dateTimeProvider.UtcNow };
                    var accAR = new Account { Id = Guid.NewGuid(), BusinessId = demoBusiness.Id, AccountCode = "1100", AccountName = "Accounts Receivable", Type = AccountType.Asset, Subtype = "Current Asset", CurrentBalance = 185000m, IsSystemAccount = true, IsActive = true, CreatedBy = "System", CreatedOn = _dateTimeProvider.UtcNow };
                    var accInv = new Account { Id = Guid.NewGuid(), BusinessId = demoBusiness.Id, AccountCode = "1200", AccountName = "Merchandise Inventory", Type = AccountType.Asset, Subtype = "Current Asset", CurrentBalance = 425000m, IsSystemAccount = true, IsActive = true, CreatedBy = "System", CreatedOn = _dateTimeProvider.UtcNow };
                    var accCgstInput = new Account { Id = Guid.NewGuid(), BusinessId = demoBusiness.Id, AccountCode = "1310", AccountName = "CGST Input Tax Credit", Type = AccountType.Asset, Subtype = "Current Asset", CurrentBalance = 2812.50m, IsSystemAccount = true, IsActive = true, CreatedBy = "System", CreatedOn = _dateTimeProvider.UtcNow };
                    var accSgstInput = new Account { Id = Guid.NewGuid(), BusinessId = demoBusiness.Id, AccountCode = "1320", AccountName = "SGST Input Tax Credit", Type = AccountType.Asset, Subtype = "Current Asset", CurrentBalance = 2812.50m, IsSystemAccount = true, IsActive = true, CreatedBy = "System", CreatedOn = _dateTimeProvider.UtcNow };
                    var accEquip = new Account { Id = Guid.NewGuid(), BusinessId = demoBusiness.Id, AccountCode = "1510", AccountName = "Plant & Machinery", Type = AccountType.Asset, Subtype = "Fixed Asset", CurrentBalance = 650000m, IsSystemAccount = false, IsActive = true, CreatedBy = "System", CreatedOn = _dateTimeProvider.UtcNow };

                    var accAP = new Account { Id = Guid.NewGuid(), BusinessId = demoBusiness.Id, AccountCode = "2010", AccountName = "Accounts Payable", Type = AccountType.Liability, Subtype = "Current Liability", CurrentBalance = 16875m, IsSystemAccount = true, IsActive = true, CreatedBy = "System", CreatedOn = _dateTimeProvider.UtcNow };
                    var accCgstOutput = new Account { Id = Guid.NewGuid(), BusinessId = demoBusiness.Id, AccountCode = "2110", AccountName = "CGST Output Payable", Type = AccountType.Liability, Subtype = "Current Liability", CurrentBalance = 864m, IsSystemAccount = true, IsActive = true, CreatedBy = "System", CreatedOn = _dateTimeProvider.UtcNow };
                    var accSgstOutput = new Account { Id = Guid.NewGuid(), BusinessId = demoBusiness.Id, AccountCode = "2120", AccountName = "SGST Output Payable", Type = AccountType.Liability, Subtype = "Current Liability", CurrentBalance = 864m, IsSystemAccount = true, IsActive = true, CreatedBy = "System", CreatedOn = _dateTimeProvider.UtcNow };
                    var accLoan = new Account { Id = Guid.NewGuid(), BusinessId = demoBusiness.Id, AccountCode = "2510", AccountName = "Term Loan - SIDBI", Type = AccountType.Liability, Subtype = "Long Term Liability", CurrentBalance = 300000m, IsSystemAccount = false, IsActive = true, CreatedBy = "System", CreatedOn = _dateTimeProvider.UtcNow };

                    var accCapital = new Account { Id = Guid.NewGuid(), BusinessId = demoBusiness.Id, AccountCode = "3010", AccountName = "Owner Capital / Equity", Type = AccountType.Equity, Subtype = "Capital", CurrentBalance = 2000000m, IsSystemAccount = true, IsActive = true, CreatedBy = "System", CreatedOn = _dateTimeProvider.UtcNow };
                    var accRetained = new Account { Id = Guid.NewGuid(), BusinessId = demoBusiness.Id, AccountCode = "3020", AccountName = "Retained Earnings", Type = AccountType.Equity, Subtype = "Reserves", CurrentBalance = 412022m, IsSystemAccount = true, IsActive = true, CreatedBy = "System", CreatedOn = _dateTimeProvider.UtcNow };

                    var accSalesRev = new Account { Id = Guid.NewGuid(), BusinessId = demoBusiness.Id, AccountCode = "4010", AccountName = "Domestic Product Sales", Type = AccountType.Revenue, Subtype = "Sales", CurrentBalance = 350000m, IsSystemAccount = true, IsActive = true, CreatedBy = "System", CreatedOn = _dateTimeProvider.UtcNow };
                    var accServiceRev = new Account { Id = Guid.NewGuid(), BusinessId = demoBusiness.Id, AccountCode = "4020", AccountName = "Installation & Service Fees", Type = AccountType.Revenue, Subtype = "Service Income", CurrentBalance = 45000m, IsSystemAccount = false, IsActive = true, CreatedBy = "System", CreatedOn = _dateTimeProvider.UtcNow };

                    var accCOGS = new Account { Id = Guid.NewGuid(), BusinessId = demoBusiness.Id, AccountCode = "5010", AccountName = "Cost of Goods Sold", Type = AccountType.Expense, Subtype = "COGS", CurrentBalance = 190000m, IsSystemAccount = true, IsActive = true, CreatedBy = "System", CreatedOn = _dateTimeProvider.UtcNow };
                    var accRent = new Account { Id = Guid.NewGuid(), BusinessId = demoBusiness.Id, AccountCode = "6010", AccountName = "Office Rent & Facility", Type = AccountType.Expense, Subtype = "Operating Expense", CurrentBalance = 75000m, IsSystemAccount = false, IsActive = true, CreatedBy = "System", CreatedOn = _dateTimeProvider.UtcNow };
                    var accSalaries = new Account { Id = Guid.NewGuid(), BusinessId = demoBusiness.Id, AccountCode = "6020", AccountName = "Staff Salaries & Wages", Type = AccountType.Expense, Subtype = "Operating Expense", CurrentBalance = 95000m, IsSystemAccount = false, IsActive = true, CreatedBy = "System", CreatedOn = _dateTimeProvider.UtcNow };
                    var accUtilities = new Account { Id = Guid.NewGuid(), BusinessId = demoBusiness.Id, AccountCode = "6030", AccountName = "Electricity & Internet", Type = AccountType.Expense, Subtype = "Operating Expense", CurrentBalance = 12000m, IsSystemAccount = false, IsActive = true, CreatedBy = "System", CreatedOn = _dateTimeProvider.UtcNow };

                    _context.Accounts.AddRange(
                        accBank, accCash, accAR, accInv, accCgstInput, accSgstInput, accEquip,
                        accAP, accCgstOutput, accSgstOutput, accLoan,
                        accCapital, accRetained,
                        accSalesRev, accServiceRev,
                        accCOGS, accRent, accSalaries, accUtilities
                    );

                    // Seed Sample Balanced Journal Voucher
                    var sampleJournal = new JournalEntry
                    {
                        Id = Guid.NewGuid(),
                        BusinessId = demoBusiness.Id,
                        EntryNumber = "JE-2026-0001",
                        EntryDate = _dateTimeProvider.UtcNow.UtcDateTime.AddDays(-2),
                        Reference = "JV-RENT-FEB",
                        Narration = "Monthly office facility rent and utility disbursement",
                        EntryType = JournalEntryType.Manual,
                        TotalDebit = 87000.00m,
                        TotalCredit = 87000.00m,
                        IsPosted = true,
                        CreatedOn = _dateTimeProvider.UtcNow.AddDays(-2),
                        CreatedBy = "System"
                    };

                    sampleJournal.Lines.Add(new JournalEntryLine
                    {
                        Id = Guid.NewGuid(),
                        JournalEntryId = sampleJournal.Id,
                        AccountId = accRent.Id,
                        Debit = 75000.00m,
                        Credit = 0m,
                        Description = "Office rent for main plant unit"
                    });
                    sampleJournal.Lines.Add(new JournalEntryLine
                    {
                        Id = Guid.NewGuid(),
                        JournalEntryId = sampleJournal.Id,
                        AccountId = accUtilities.Id,
                        Debit = 12000.00m,
                        Credit = 0m,
                        Description = "Electricity and high-speed broadband charges"
                    });
                    sampleJournal.Lines.Add(new JournalEntryLine
                    {
                        Id = Guid.NewGuid(),
                        JournalEntryId = sampleJournal.Id,
                        AccountId = accBank.Id,
                        Debit = 0m,
                        Credit = 87000.00m,
                        Description = "Disbursed via HDFC net banking payment"
                    });

                    _context.JournalEntries.Add(sampleJournal);
                }

                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Seeded Demo Tenant: Acme Global Ltd (ACME-01) with inventory, sales, procurement, and accounting ledger.");

                // Seed Cloth Hub Retail Masters & Catalog for Demo Business
                var hasClothData = await _context.ClothSizes
                    .IgnoreQueryFilters()
                    .AnyAsync(s => s.BusinessId == demoBusiness.Id, cancellationToken);

                if (!hasClothData)
                {
                    await SeedClothHubDefaultsAsync(demoBusiness.Id, cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while seeding database.");
            throw;
        }
    }

    private async Task SeedClothHubDefaultsAsync(Guid businessId, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Seeding default Cloth Hub retail masters & catalog for business: {BusinessId}", businessId);

            // 1. Standard Sizes
            var sizes = new List<ClothSize>
            {
                new() { Id = Guid.NewGuid(), BusinessId = businessId, Name = "S", Code = "S", CategoryType = "Standard", SortOrder = 1, CreatedOn = _dateTimeProvider.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), BusinessId = businessId, Name = "M", Code = "M", CategoryType = "Standard", SortOrder = 2, CreatedOn = _dateTimeProvider.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), BusinessId = businessId, Name = "L", Code = "L", CategoryType = "Standard", SortOrder = 3, CreatedOn = _dateTimeProvider.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), BusinessId = businessId, Name = "XL", Code = "XL", CategoryType = "Standard", SortOrder = 4, CreatedOn = _dateTimeProvider.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), BusinessId = businessId, Name = "XXL", Code = "XXL", CategoryType = "Standard", SortOrder = 5, CreatedOn = _dateTimeProvider.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), BusinessId = businessId, Name = "30", Code = "30", CategoryType = "Waist", SortOrder = 6, CreatedOn = _dateTimeProvider.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), BusinessId = businessId, Name = "32", Code = "32", CategoryType = "Waist", SortOrder = 7, CreatedOn = _dateTimeProvider.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), BusinessId = businessId, Name = "34", Code = "34", CategoryType = "Waist", SortOrder = 8, CreatedOn = _dateTimeProvider.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), BusinessId = businessId, Name = "36", Code = "36", CategoryType = "Waist", SortOrder = 9, CreatedOn = _dateTimeProvider.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), BusinessId = businessId, Name = "Free Size", Code = "FS", CategoryType = "FreeSize", SortOrder = 10, CreatedOn = _dateTimeProvider.UtcNow, CreatedBy = "System" }
            };
            _context.ClothSizes.AddRange(sizes);

            // 2. Standard Colours
            var colours = new List<ClothColour>
            {
                new() { Id = Guid.NewGuid(), BusinessId = businessId, Name = "Jet Black", HexCode = "#111111", PaletteGroup = "Dark", CreatedOn = _dateTimeProvider.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), BusinessId = businessId, Name = "Crisp White", HexCode = "#FFFFFF", PaletteGroup = "Light", CreatedOn = _dateTimeProvider.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), BusinessId = businessId, Name = "Navy Blue", HexCode = "#001F3F", PaletteGroup = "Dark", CreatedOn = _dateTimeProvider.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), BusinessId = businessId, Name = "Olive Green", HexCode = "#3D9970", PaletteGroup = "Primary", CreatedOn = _dateTimeProvider.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), BusinessId = businessId, Name = "Wine Red", HexCode = "#85144B", PaletteGroup = "Dark", CreatedOn = _dateTimeProvider.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), BusinessId = businessId, Name = "Sky Blue", HexCode = "#7FDBFF", PaletteGroup = "Pastel", CreatedOn = _dateTimeProvider.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), BusinessId = businessId, Name = "Heather Grey", HexCode = "#AAAAAA", PaletteGroup = "Light", CreatedOn = _dateTimeProvider.UtcNow, CreatedBy = "System" }
            };
            _context.ClothColours.AddRange(colours);

            // 3. Brands
            var brandRaymond = new ClothBrand { Id = Guid.NewGuid(), BusinessId = businessId, Name = "Raymond", Code = "RAY", Description = "Premium formal menswear & fabrics", CreatedOn = _dateTimeProvider.UtcNow, CreatedBy = "System" };
            var brandPeter = new ClothBrand { Id = Guid.NewGuid(), BusinessId = businessId, Name = "Peter England", Code = "PE", Description = "Trusted formal and casual workwear", CreatedOn = _dateTimeProvider.UtcNow, CreatedBy = "System" };
            var brandLevis = new ClothBrand { Id = Guid.NewGuid(), BusinessId = businessId, Name = "Levi's", Code = "LEV", Description = "Authentic denim jeans & casual wear", CreatedOn = _dateTimeProvider.UtcNow, CreatedBy = "System" };
            var brandZara = new ClothBrand { Id = Guid.NewGuid(), BusinessId = businessId, Name = "Zara Man", Code = "ZR", Description = "Contemporary fashion apparel", CreatedOn = _dateTimeProvider.UtcNow, CreatedBy = "System" };
            _context.ClothBrands.AddRange(brandRaymond, brandPeter, brandLevis, brandZara);

            // 4. Categories
            var catShirts = new ClothCategory { Id = Guid.NewGuid(), BusinessId = businessId, Name = "Men's Formal Shirts", Code = "MSHRT", Gender = "Men's", CreatedOn = _dateTimeProvider.UtcNow, CreatedBy = "System" };
            var catJeans = new ClothCategory { Id = Guid.NewGuid(), BusinessId = businessId, Name = "Men's Denim Jeans", Code = "MJN", Gender = "Men's", CreatedOn = _dateTimeProvider.UtcNow, CreatedBy = "System" };
            var catKurtis = new ClothCategory { Id = Guid.NewGuid(), BusinessId = businessId, Name = "Women's Kurtis", Code = "WKT", Gender = "Women's", CreatedOn = _dateTimeProvider.UtcNow, CreatedBy = "System" };
            var catTshirts = new ClothCategory { Id = Guid.NewGuid(), BusinessId = businessId, Name = "Casual T-Shirts & Polos", Code = "TSH", Gender = "Unisex", CreatedOn = _dateTimeProvider.UtcNow, CreatedBy = "System" };
            _context.ClothCategories.AddRange(catShirts, catJeans, catKurtis, catTshirts);

            // 5. Fabrics & Designs
            var fabricCotton = new ClothFabric { Id = Guid.NewGuid(), BusinessId = businessId, Name = "100% Combed Cotton", Composition = "100% Cotton", CreatedOn = _dateTimeProvider.UtcNow, CreatedBy = "System" };
            var fabricLinen = new ClothFabric { Id = Guid.NewGuid(), BusinessId = businessId, Name = "Linen Blend", Composition = "70% Linen, 30% Cotton", CreatedOn = _dateTimeProvider.UtcNow, CreatedBy = "System" };
            var fabricDenim = new ClothFabric { Id = Guid.NewGuid(), BusinessId = businessId, Name = "Stretch Denim", Composition = "98% Cotton, 2% Elastane", CreatedOn = _dateTimeProvider.UtcNow, CreatedBy = "System" };
            _context.ClothFabrics.AddRange(fabricCotton, fabricLinen, fabricDenim);

            var designSolid = new ClothDesign { Id = Guid.NewGuid(), BusinessId = businessId, DesignNumber = "DS-SOLID-01", Pattern = "Plain", Fit = "Regular", Season = "All Season", CreatedOn = _dateTimeProvider.UtcNow, CreatedBy = "System" };
            var designCheck = new ClothDesign { Id = Guid.NewGuid(), BusinessId = businessId, DesignNumber = "DS-CHECK-02", Pattern = "Checked", Fit = "Slim Fit", Season = "Autumn", CreatedOn = _dateTimeProvider.UtcNow, CreatedBy = "System" };
            _context.ClothDesigns.AddRange(designSolid, designCheck);

            // 6. Sample Product & Variants
            var sampleShirt = new ClothProduct
            {
                Id = Guid.NewGuid(),
                BusinessId = businessId,
                Name = "Raymond Oxford Formal Shirt",
                SkuPrefix = "RAY-OXF",
                Description = "Premium 100% combed cotton formal shirt with tailored fit and button-down collar.",
                BrandId = brandRaymond.Id,
                CategoryId = catShirts.Id,
                FabricId = fabricCotton.Id,
                DesignNumber = "DS-SOLID-01",
                Gender = "Men's",
                HsnCode = "6109",
                GstRate = 5.0m,
                UnitOfMeasurement = "PCS",
                BaseMrp = 1999m,
                BasePurchasePrice = 850m,
                BaseSellingPrice = 1499m,
                MinStockLevel = 10,
                IsActive = true,
                CreatedOn = _dateTimeProvider.UtcNow,
                CreatedBy = "System"
            };
            _context.ClothProducts.Add(sampleShirt);

            var sizeS = sizes.First(s => s.Name == "S");
            var sizeM = sizes.First(s => s.Name == "M");
            var sizeL = sizes.First(s => s.Name == "L");
            var sizeXL = sizes.First(s => s.Name == "XL");

            var colNavy = colours.First(c => c.Name == "Navy Blue");
            var colWhite = colours.First(c => c.Name == "Crisp White");
            var colBlack = colours.First(c => c.Name == "Jet Black");

            var variants = new List<ClothProductVariant>
            {
                new() { Id = Guid.NewGuid(), BusinessId = businessId, ClothProductId = sampleShirt.Id, SizeId = sizeS.Id, ColourId = colNavy.Id, Sku = "RAY-OXF-NAVY-S", Barcode = "890123400001", Mrp = 1999m, PurchasePrice = 850m, SellingPrice = 1499m, CurrentStock = 18, MinStockLevel = 5, CreatedOn = _dateTimeProvider.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), BusinessId = businessId, ClothProductId = sampleShirt.Id, SizeId = sizeM.Id, ColourId = colNavy.Id, Sku = "RAY-OXF-NAVY-M", Barcode = "890123400002", Mrp = 1999m, PurchasePrice = 850m, SellingPrice = 1499m, CurrentStock = 32, MinStockLevel = 5, CreatedOn = _dateTimeProvider.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), BusinessId = businessId, ClothProductId = sampleShirt.Id, SizeId = sizeL.Id, ColourId = colNavy.Id, Sku = "RAY-OXF-NAVY-L", Barcode = "890123400003", Mrp = 1999m, PurchasePrice = 850m, SellingPrice = 1499m, CurrentStock = 24, MinStockLevel = 5, CreatedOn = _dateTimeProvider.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), BusinessId = businessId, ClothProductId = sampleShirt.Id, SizeId = sizeXL.Id, ColourId = colNavy.Id, Sku = "RAY-OXF-NAVY-XL", Barcode = "890123400004", Mrp = 1999m, PurchasePrice = 850m, SellingPrice = 1499m, CurrentStock = 14, MinStockLevel = 5, CreatedOn = _dateTimeProvider.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), BusinessId = businessId, ClothProductId = sampleShirt.Id, SizeId = sizeM.Id, ColourId = colWhite.Id, Sku = "RAY-OXF-WHT-M", Barcode = "890123400005", Mrp = 1999m, PurchasePrice = 850m, SellingPrice = 1499m, CurrentStock = 28, MinStockLevel = 5, CreatedOn = _dateTimeProvider.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), BusinessId = businessId, ClothProductId = sampleShirt.Id, SizeId = sizeL.Id, ColourId = colWhite.Id, Sku = "RAY-OXF-WHT-L", Barcode = "890123400006", Mrp = 1999m, PurchasePrice = 850m, SellingPrice = 1499m, CurrentStock = 22, MinStockLevel = 5, CreatedOn = _dateTimeProvider.UtcNow, CreatedBy = "System" },
                new() { Id = Guid.NewGuid(), BusinessId = businessId, ClothProductId = sampleShirt.Id, SizeId = sizeM.Id, ColourId = colBlack.Id, Sku = "RAY-OXF-BLK-M", Barcode = "890123400007", Mrp = 1999m, PurchasePrice = 850m, SellingPrice = 1499m, CurrentStock = 20, MinStockLevel = 5, CreatedOn = _dateTimeProvider.UtcNow, CreatedBy = "System" }
            };
            _context.ClothProductVariants.AddRange(variants);

            // 7. Sample Supplier & Customer
            var supplier = new ClothSupplier
            {
                Id = Guid.NewGuid(),
                BusinessId = businessId,
                SupplierName = "Raymond Apparel Mills & Distributors",
                ContactPerson = "Vikram Singhania",
                Phone = "+919811223344",
                Email = "orders@raymondapparel.in",
                Gstin = "27AAACR1234M1Z8",
                Address = "Plot 12, Textile SEZ, Bhiwandi",
                City = "Mumbai",
                State = "Maharashtra",
                Pincode = "421302",
                PaymentTerms = "Net 30 Days",
                CreditLimit = 500000m,
                CurrentPayableBalance = 42500m,
                IsActive = true,
                CreatedOn = _dateTimeProvider.UtcNow,
                CreatedBy = "System"
            };
            _context.ClothSuppliers.Add(supplier);

            var customer = new ClothCustomer
            {
                Id = Guid.NewGuid(),
                BusinessId = businessId,
                CustomerName = "Rahul Sharma",
                Phone = "+919876543210",
                Email = "rahul.sharma@gmail.com",
                Address = "Flat 402, Green Heights, Andheri West",
                City = "Mumbai",
                CreditLimit = 10000m,
                CurrentOutstanding = 0m,
                TotalSpentAmount = 8990m,
                TotalVisitsCount = 4,
                IsActive = true,
                CreatedOn = _dateTimeProvider.UtcNow,
                CreatedBy = "System"
            };
            _context.ClothCustomers.Add(customer);

            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Successfully seeded Cloth Hub retail demo catalog (Raymond shirts, sizes, colours, variants).");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding default Cloth Hub data.");
        }
    }
}
