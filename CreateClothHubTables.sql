-- =========================================================================
-- BIZFLOW ERP: CLOTH HUB MODULE DATABASE SCHEMA SCRIPT
-- Idempotent table creation script for Microsoft SQL Server
-- Run this script in SQL Server Management Studio (SSMS) on your BizFlow DB
-- =========================================================================

-- 1. ClothBrands
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ClothBrands')
BEGIN
    CREATE TABLE [dbo].[ClothBrands] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        [BusinessId] UNIQUEIDENTIFIER NOT NULL,
        [Name] NVARCHAR(250) NOT NULL,
        [Code] NVARCHAR(50) NOT NULL,
        [Description] NVARCHAR(MAX) NULL,
        [LogoUrl] NVARCHAR(500) NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedOn] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        [CreatedBy] NVARCHAR(256) NULL,
        [UpdatedOn] DATETIMEOFFSET NULL,
        [UpdatedBy] NVARCHAR(256) NULL,
        [IsDeleted] BIT NOT NULL DEFAULT 0
    );
    CREATE INDEX [IX_ClothBrands_BusinessId] ON [dbo].[ClothBrands]([BusinessId]);
END
GO

-- 2. ClothCategories
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ClothCategories')
BEGIN
    CREATE TABLE [dbo].[ClothCategories] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        [BusinessId] UNIQUEIDENTIFIER NOT NULL,
        [Name] NVARCHAR(250) NOT NULL,
        [Code] NVARCHAR(50) NOT NULL,
        [Gender] NVARCHAR(50) NOT NULL DEFAULT 'Unisex',
        [ParentCategoryId] UNIQUEIDENTIFIER NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedOn] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        [CreatedBy] NVARCHAR(256) NULL,
        [UpdatedOn] DATETIMEOFFSET NULL,
        [UpdatedBy] NVARCHAR(256) NULL,
        [IsDeleted] BIT NOT NULL DEFAULT 0
    );
    CREATE INDEX [IX_ClothCategories_BusinessId] ON [dbo].[ClothCategories]([BusinessId]);
END
GO

-- 3. ClothSizes
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ClothSizes')
BEGIN
    CREATE TABLE [dbo].[ClothSizes] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        [BusinessId] UNIQUEIDENTIFIER NOT NULL,
        [Name] NVARCHAR(100) NOT NULL,
        [Code] NVARCHAR(50) NOT NULL,
        [CategoryType] NVARCHAR(50) NOT NULL DEFAULT 'Standard',
        [SortOrder] INT NOT NULL DEFAULT 0,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedOn] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        [CreatedBy] NVARCHAR(256) NULL,
        [UpdatedOn] DATETIMEOFFSET NULL,
        [UpdatedBy] NVARCHAR(256) NULL,
        [IsDeleted] BIT NOT NULL DEFAULT 0
    );
    CREATE INDEX [IX_ClothSizes_BusinessId] ON [dbo].[ClothSizes]([BusinessId]);
END
GO

-- 4. ClothColours
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ClothColours')
BEGIN
    CREATE TABLE [dbo].[ClothColours] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        [BusinessId] UNIQUEIDENTIFIER NOT NULL,
        [Name] NVARCHAR(100) NOT NULL,
        [HexCode] NVARCHAR(20) NOT NULL DEFAULT '#000000',
        [PaletteGroup] NVARCHAR(50) NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedOn] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        [CreatedBy] NVARCHAR(256) NULL,
        [UpdatedOn] DATETIMEOFFSET NULL,
        [UpdatedBy] NVARCHAR(256) NULL,
        [IsDeleted] BIT NOT NULL DEFAULT 0
    );
    CREATE INDEX [IX_ClothColours_BusinessId] ON [dbo].[ClothColours]([BusinessId]);
END
GO

-- 5. ClothDesigns
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ClothDesigns')
BEGIN
    CREATE TABLE [dbo].[ClothDesigns] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        [BusinessId] UNIQUEIDENTIFIER NOT NULL,
        [DesignNumber] NVARCHAR(100) NOT NULL,
        [Pattern] NVARCHAR(100) NOT NULL DEFAULT 'Plain',
        [Fit] NVARCHAR(100) NOT NULL DEFAULT 'Regular',
        [Season] NVARCHAR(100) NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedOn] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        [CreatedBy] NVARCHAR(256) NULL,
        [UpdatedOn] DATETIMEOFFSET NULL,
        [UpdatedBy] NVARCHAR(256) NULL,
        [IsDeleted] BIT NOT NULL DEFAULT 0
    );
    CREATE INDEX [IX_ClothDesigns_BusinessId] ON [dbo].[ClothDesigns]([BusinessId]);
END
GO

-- 6. ClothFabrics
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ClothFabrics')
BEGIN
    CREATE TABLE [dbo].[ClothFabrics] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        [BusinessId] UNIQUEIDENTIFIER NOT NULL,
        [Name] NVARCHAR(250) NOT NULL,
        [Composition] NVARCHAR(250) NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedOn] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        [CreatedBy] NVARCHAR(256) NULL,
        [UpdatedOn] DATETIMEOFFSET NULL,
        [UpdatedBy] NVARCHAR(256) NULL,
        [IsDeleted] BIT NOT NULL DEFAULT 0
    );
    CREATE INDEX [IX_ClothFabrics_BusinessId] ON [dbo].[ClothFabrics]([BusinessId]);
END
GO

-- 7. ClothProducts
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ClothProducts')
BEGIN
    CREATE TABLE [dbo].[ClothProducts] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        [BusinessId] UNIQUEIDENTIFIER NOT NULL,
        [Name] NVARCHAR(250) NOT NULL,
        [SkuPrefix] NVARCHAR(100) NOT NULL,
        [Description] NVARCHAR(MAX) NULL,
        [BrandId] UNIQUEIDENTIFIER NOT NULL,
        [CategoryId] UNIQUEIDENTIFIER NOT NULL,
        [FabricId] UNIQUEIDENTIFIER NULL,
        [DesignNumber] NVARCHAR(100) NULL,
        [Gender] NVARCHAR(50) NOT NULL DEFAULT 'Unisex',
        [Season] NVARCHAR(100) NULL,
        [Collection] NVARCHAR(100) NULL,
        [HsnCode] NVARCHAR(20) NOT NULL DEFAULT '6109',
        [GstRate] DECIMAL(18,2) NOT NULL DEFAULT 5.0,
        [UnitOfMeasurement] NVARCHAR(20) NOT NULL DEFAULT 'PCS',
        [BaseMrp] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [BasePurchasePrice] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [BaseSellingPrice] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [WholesalePrice] DECIMAL(18,2) NULL,
        [MinStockLevel] INT NOT NULL DEFAULT 10,
        [ImageUrl] NVARCHAR(500) NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedOn] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        [CreatedBy] NVARCHAR(256) NULL,
        [UpdatedOn] DATETIMEOFFSET NULL,
        [UpdatedBy] NVARCHAR(256) NULL,
        [IsDeleted] BIT NOT NULL DEFAULT 0
    );
    CREATE INDEX [IX_ClothProducts_BusinessId] ON [dbo].[ClothProducts]([BusinessId]);
    CREATE INDEX [IX_ClothProducts_BrandId] ON [dbo].[ClothProducts]([BrandId]);
    CREATE INDEX [IX_ClothProducts_CategoryId] ON [dbo].[ClothProducts]([CategoryId]);
END
GO

-- 8. ClothProductVariants
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ClothProductVariants')
BEGIN
    CREATE TABLE [dbo].[ClothProductVariants] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        [BusinessId] UNIQUEIDENTIFIER NOT NULL,
        [ClothProductId] UNIQUEIDENTIFIER NOT NULL,
        [SizeId] UNIQUEIDENTIFIER NOT NULL,
        [ColourId] UNIQUEIDENTIFIER NOT NULL,
        [Sku] NVARCHAR(100) NOT NULL,
        [Barcode] NVARCHAR(100) NOT NULL,
        [Mrp] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [PurchasePrice] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [SellingPrice] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [CurrentStock] INT NOT NULL DEFAULT 0,
        [ReservedStock] INT NOT NULL DEFAULT 0,
        [MinStockLevel] INT NOT NULL DEFAULT 5,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedOn] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        [CreatedBy] NVARCHAR(256) NULL,
        [UpdatedOn] DATETIMEOFFSET NULL,
        [UpdatedBy] NVARCHAR(256) NULL,
        [IsDeleted] BIT NOT NULL DEFAULT 0
    );
    CREATE INDEX [IX_ClothProductVariants_BusinessId] ON [dbo].[ClothProductVariants]([BusinessId]);
    CREATE INDEX [IX_ClothProductVariants_ClothProductId] ON [dbo].[ClothProductVariants]([ClothProductId]);
    CREATE INDEX [IX_ClothProductVariants_Barcode] ON [dbo].[ClothProductVariants]([Barcode]);
    CREATE INDEX [IX_ClothProductVariants_Sku] ON [dbo].[ClothProductVariants]([Sku]);
END
GO

-- 9. ClothBoxPacks
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ClothBoxPacks')
BEGIN
    CREATE TABLE [dbo].[ClothBoxPacks] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        [BusinessId] UNIQUEIDENTIFIER NOT NULL,
        [ClothProductId] UNIQUEIDENTIFIER NOT NULL,
        [PackName] NVARCHAR(250) NOT NULL,
        [PackCode] NVARCHAR(100) NOT NULL,
        [Barcode] NVARCHAR(100) NOT NULL,
        [PiecesPerPack] INT NOT NULL DEFAULT 12,
        [CostPrice] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [SellingPrice] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [Mrp] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [CurrentBoxStock] INT NOT NULL DEFAULT 0,
        [AssortmentDescription] NVARCHAR(MAX) NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedOn] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        [CreatedBy] NVARCHAR(256) NULL,
        [UpdatedOn] DATETIMEOFFSET NULL,
        [UpdatedBy] NVARCHAR(256) NULL,
        [IsDeleted] BIT NOT NULL DEFAULT 0
    );
    CREATE INDEX [IX_ClothBoxPacks_BusinessId] ON [dbo].[ClothBoxPacks]([BusinessId]);
    CREATE INDEX [IX_ClothBoxPacks_ClothProductId] ON [dbo].[ClothBoxPacks]([ClothProductId]);
END
GO

-- 10. ClothBoxPackItems
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ClothBoxPackItems')
BEGIN
    CREATE TABLE [dbo].[ClothBoxPackItems] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        [ClothBoxPackId] UNIQUEIDENTIFIER NOT NULL,
        [ClothProductVariantId] UNIQUEIDENTIFIER NOT NULL,
        [QuantityPerBox] INT NOT NULL DEFAULT 1,
        [CreatedOn] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        [CreatedBy] NVARCHAR(256) NULL,
        [UpdatedOn] DATETIMEOFFSET NULL,
        [UpdatedBy] NVARCHAR(256) NULL,
        [IsDeleted] BIT NOT NULL DEFAULT 0
    );
    CREATE INDEX [IX_ClothBoxPackItems_ClothBoxPackId] ON [dbo].[ClothBoxPackItems]([ClothBoxPackId]);
END
GO

-- 11. ClothStockAdjustments
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ClothStockAdjustments')
BEGIN
    CREATE TABLE [dbo].[ClothStockAdjustments] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        [BusinessId] UNIQUEIDENTIFIER NOT NULL,
        [AdjustmentNumber] NVARCHAR(100) NOT NULL,
        [AdjustmentDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [Reason] NVARCHAR(250) NOT NULL DEFAULT 'Physical Count Reconcile',
        [TotalItemsCount] INT NOT NULL DEFAULT 0,
        [TotalNetQuantityDiff] INT NOT NULL DEFAULT 0,
        [TotalValueImpact] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [Remarks] NVARCHAR(MAX) NULL,
        [CreatedOn] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        [CreatedBy] NVARCHAR(256) NULL,
        [UpdatedOn] DATETIMEOFFSET NULL,
        [UpdatedBy] NVARCHAR(256) NULL,
        [IsDeleted] BIT NOT NULL DEFAULT 0
    );
    CREATE INDEX [IX_ClothStockAdjustments_BusinessId] ON [dbo].[ClothStockAdjustments]([BusinessId]);
END
GO

-- 12. ClothStockAdjustmentItems
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ClothStockAdjustmentItems')
BEGIN
    CREATE TABLE [dbo].[ClothStockAdjustmentItems] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        [ClothStockAdjustmentId] UNIQUEIDENTIFIER NOT NULL,
        [ClothProductVariantId] UNIQUEIDENTIFIER NOT NULL,
        [PreviousStock] INT NOT NULL DEFAULT 0,
        [AdjustedStock] INT NOT NULL DEFAULT 0,
        [Difference] INT NOT NULL DEFAULT 0,
        [UnitCost] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [TotalValueImpact] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [CreatedOn] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        [CreatedBy] NVARCHAR(256) NULL,
        [UpdatedOn] DATETIMEOFFSET NULL,
        [UpdatedBy] NVARCHAR(256) NULL,
        [IsDeleted] BIT NOT NULL DEFAULT 0
    );
    CREATE INDEX [IX_ClothStockAdjustmentItems_ClothStockAdjustmentId] ON [dbo].[ClothStockAdjustmentItems]([ClothStockAdjustmentId]);
END
GO

-- 13. ClothStockLedgers
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ClothStockLedgers')
BEGIN
    CREATE TABLE [dbo].[ClothStockLedgers] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        [BusinessId] UNIQUEIDENTIFIER NOT NULL,
        [ClothProductVariantId] UNIQUEIDENTIFIER NOT NULL,
        [TransactionDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [TransactionType] NVARCHAR(100) NOT NULL DEFAULT 'Adjustment',
        [ReferenceNumber] NVARCHAR(100) NOT NULL DEFAULT '',
        [QuantityIn] INT NOT NULL DEFAULT 0,
        [QuantityOut] INT NOT NULL DEFAULT 0,
        [RunningBalance] INT NOT NULL DEFAULT 0,
        [UnitCost] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [Notes] NVARCHAR(MAX) NULL,
        [CreatedOn] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        [CreatedBy] NVARCHAR(256) NULL,
        [UpdatedOn] DATETIMEOFFSET NULL,
        [UpdatedBy] NVARCHAR(256) NULL,
        [IsDeleted] BIT NOT NULL DEFAULT 0
    );
    CREATE INDEX [IX_ClothStockLedgers_BusinessId] ON [dbo].[ClothStockLedgers]([BusinessId]);
    CREATE INDEX [IX_ClothStockLedgers_ClothProductVariantId] ON [dbo].[ClothStockLedgers]([ClothProductVariantId]);
END
GO

-- 14. ClothSuppliers
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ClothSuppliers')
BEGIN
    CREATE TABLE [dbo].[ClothSuppliers] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        [BusinessId] UNIQUEIDENTIFIER NOT NULL,
        [SupplierName] NVARCHAR(250) NOT NULL,
        [ContactPerson] NVARCHAR(200) NULL,
        [Phone] NVARCHAR(50) NULL,
        [Email] NVARCHAR(200) NULL,
        [Gstin] NVARCHAR(50) NULL,
        [Address] NVARCHAR(500) NULL,
        [City] NVARCHAR(100) NULL,
        [State] NVARCHAR(100) NULL,
        [Pincode] NVARCHAR(20) NULL,
        [PaymentTerms] NVARCHAR(100) NOT NULL DEFAULT 'Net 30 Days',
        [CreditLimit] DECIMAL(18,2) NOT NULL DEFAULT 500000,
        [CurrentPayableBalance] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedOn] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        [CreatedBy] NVARCHAR(256) NULL,
        [UpdatedOn] DATETIMEOFFSET NULL,
        [UpdatedBy] NVARCHAR(256) NULL,
        [IsDeleted] BIT NOT NULL DEFAULT 0
    );
    CREATE INDEX [IX_ClothSuppliers_BusinessId] ON [dbo].[ClothSuppliers]([BusinessId]);
END
GO

-- 15. ClothPurchaseBills
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ClothPurchaseBills')
BEGIN
    CREATE TABLE [dbo].[ClothPurchaseBills] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        [BusinessId] UNIQUEIDENTIFIER NOT NULL,
        [ClothSupplierId] UNIQUEIDENTIFIER NOT NULL,
        [BillNumber] NVARCHAR(100) NOT NULL,
        [SupplierInvoiceNumber] NVARCHAR(100) NOT NULL DEFAULT '',
        [BillDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [DueDate] DATETIME2 NULL,
        [SubTotal] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [TaxAmount] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [DiscountAmount] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [RoundOff] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [TotalAmount] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [PaidAmount] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [BalanceAmount] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [PaymentStatus] NVARCHAR(50) NOT NULL DEFAULT 'Unpaid',
        [InwardStatus] NVARCHAR(50) NOT NULL DEFAULT 'Received',
        [Notes] NVARCHAR(MAX) NULL,
        [CreatedOn] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        [CreatedBy] NVARCHAR(256) NULL,
        [UpdatedOn] DATETIMEOFFSET NULL,
        [UpdatedBy] NVARCHAR(256) NULL,
        [IsDeleted] BIT NOT NULL DEFAULT 0
    );
    CREATE INDEX [IX_ClothPurchaseBills_BusinessId] ON [dbo].[ClothPurchaseBills]([BusinessId]);
    CREATE INDEX [IX_ClothPurchaseBills_ClothSupplierId] ON [dbo].[ClothPurchaseBills]([ClothSupplierId]);
END
GO

-- 16. ClothPurchaseBillItems
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ClothPurchaseBillItems')
BEGIN
    CREATE TABLE [dbo].[ClothPurchaseBillItems] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        [ClothPurchaseBillId] UNIQUEIDENTIFIER NOT NULL,
        [ClothProductVariantId] UNIQUEIDENTIFIER NOT NULL,
        [Quantity] INT NOT NULL DEFAULT 1,
        [UnitCost] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [TaxPercent] DECIMAL(18,2) NOT NULL DEFAULT 5.0,
        [TaxAmount] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [TotalAmount] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [CreatedOn] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        [CreatedBy] NVARCHAR(256) NULL,
        [UpdatedOn] DATETIMEOFFSET NULL,
        [UpdatedBy] NVARCHAR(256) NULL,
        [IsDeleted] BIT NOT NULL DEFAULT 0
    );
    CREATE INDEX [IX_ClothPurchaseBillItems_ClothPurchaseBillId] ON [dbo].[ClothPurchaseBillItems]([ClothPurchaseBillId]);
END
GO

-- 17. ClothPurchaseReturns
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ClothPurchaseReturns')
BEGIN
    CREATE TABLE [dbo].[ClothPurchaseReturns] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        [BusinessId] UNIQUEIDENTIFIER NOT NULL,
        [ClothSupplierId] UNIQUEIDENTIFIER NOT NULL,
        [ReturnNumber] NVARCHAR(100) NOT NULL,
        [ReturnDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [OriginalBillNumber] NVARCHAR(100) NULL,
        [TotalAmount] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [Reason] NVARCHAR(250) NOT NULL DEFAULT 'Defective / Damaged',
        [Notes] NVARCHAR(MAX) NULL,
        [CreatedOn] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        [CreatedBy] NVARCHAR(256) NULL,
        [UpdatedOn] DATETIMEOFFSET NULL,
        [UpdatedBy] NVARCHAR(256) NULL,
        [IsDeleted] BIT NOT NULL DEFAULT 0
    );
    CREATE INDEX [IX_ClothPurchaseReturns_BusinessId] ON [dbo].[ClothPurchaseReturns]([BusinessId]);
END
GO

-- 18. ClothPurchaseReturnItems
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ClothPurchaseReturnItems')
BEGIN
    CREATE TABLE [dbo].[ClothPurchaseReturnItems] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        [ClothPurchaseReturnId] UNIQUEIDENTIFIER NOT NULL,
        [ClothProductVariantId] UNIQUEIDENTIFIER NOT NULL,
        [Quantity] INT NOT NULL DEFAULT 1,
        [UnitCost] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [TotalAmount] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [CreatedOn] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        [CreatedBy] NVARCHAR(256) NULL,
        [UpdatedOn] DATETIMEOFFSET NULL,
        [UpdatedBy] NVARCHAR(256) NULL,
        [IsDeleted] BIT NOT NULL DEFAULT 0
    );
    CREATE INDEX [IX_ClothPurchaseReturnItems_ClothPurchaseReturnId] ON [dbo].[ClothPurchaseReturnItems]([ClothPurchaseReturnId]);
END
GO

-- 19. ClothSalesInvoices
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ClothSalesInvoices')
BEGIN
    CREATE TABLE [dbo].[ClothSalesInvoices] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        [BusinessId] UNIQUEIDENTIFIER NOT NULL,
        [InvoiceNumber] NVARCHAR(100) NOT NULL,
        [InvoiceDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [CustomerName] NVARCHAR(200) NOT NULL DEFAULT 'Walk-in Customer',
        [CustomerPhone] NVARCHAR(50) NULL,
        [SubTotal] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [DiscountAmount] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [TaxAmount] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [RoundOff] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [TotalAmount] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [PaymentMode] NVARCHAR(50) NOT NULL DEFAULT 'Cash',
        [PaymentStatus] NVARCHAR(50) NOT NULL DEFAULT 'Paid',
        [CashTendered] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [ChangeReturned] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [SalesPerson] NVARCHAR(200) NULL,
        [Notes] NVARCHAR(MAX) NULL,
        [CreatedOn] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        [CreatedBy] NVARCHAR(256) NULL,
        [UpdatedOn] DATETIMEOFFSET NULL,
        [UpdatedBy] NVARCHAR(256) NULL,
        [IsDeleted] BIT NOT NULL DEFAULT 0
    );
    CREATE INDEX [IX_ClothSalesInvoices_BusinessId] ON [dbo].[ClothSalesInvoices]([BusinessId]);
    CREATE INDEX [IX_ClothSalesInvoices_InvoiceNumber] ON [dbo].[ClothSalesInvoices]([InvoiceNumber]);
END
GO

-- 20. ClothSalesInvoiceItems
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ClothSalesInvoiceItems')
BEGIN
    CREATE TABLE [dbo].[ClothSalesInvoiceItems] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        [ClothSalesInvoiceId] UNIQUEIDENTIFIER NOT NULL,
        [ClothProductVariantId] UNIQUEIDENTIFIER NOT NULL,
        [ItemDescription] NVARCHAR(250) NOT NULL,
        [Sku] NVARCHAR(100) NOT NULL,
        [Barcode] NVARCHAR(100) NOT NULL,
        [SizeName] NVARCHAR(50) NULL,
        [ColourName] NVARCHAR(50) NULL,
        [Quantity] INT NOT NULL DEFAULT 1,
        [UnitPrice] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [DiscountAmount] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [TaxPercent] DECIMAL(18,2) NOT NULL DEFAULT 5.0,
        [TaxAmount] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [LineTotal] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [CreatedOn] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        [CreatedBy] NVARCHAR(256) NULL,
        [UpdatedOn] DATETIMEOFFSET NULL,
        [UpdatedBy] NVARCHAR(256) NULL,
        [IsDeleted] BIT NOT NULL DEFAULT 0
    );
    CREATE INDEX [IX_ClothSalesInvoiceItems_ClothSalesInvoiceId] ON [dbo].[ClothSalesInvoiceItems]([ClothSalesInvoiceId]);
END
GO

-- 21. ClothSalesReturns
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ClothSalesReturns')
BEGIN
    CREATE TABLE [dbo].[ClothSalesReturns] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        [BusinessId] UNIQUEIDENTIFIER NOT NULL,
        [ReturnNumber] NVARCHAR(100) NOT NULL,
        [ReturnDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [ClothSalesInvoiceId] UNIQUEIDENTIFIER NULL,
        [OriginalInvoiceNumber] NVARCHAR(100) NULL,
        [CustomerName] NVARCHAR(200) NOT NULL DEFAULT 'Walk-in Customer',
        [CustomerPhone] NVARCHAR(50) NULL,
        [ReturnType] NVARCHAR(50) NOT NULL DEFAULT 'Exchange',
        [TotalReturnAmount] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [TotalExchangeAmount] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [NetDifference] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [SettlementMode] NVARCHAR(50) NOT NULL DEFAULT 'EvenExchange',
        [CreditNoteNumber] NVARCHAR(100) NULL,
        [Reason] NVARCHAR(250) NOT NULL DEFAULT 'Size / Fit Issue',
        [Remarks] NVARCHAR(MAX) NULL,
        [HandledBy] NVARCHAR(200) NULL,
        [CreatedOn] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        [CreatedBy] NVARCHAR(256) NULL,
        [UpdatedOn] DATETIMEOFFSET NULL,
        [UpdatedBy] NVARCHAR(256) NULL,
        [IsDeleted] BIT NOT NULL DEFAULT 0
    );
    CREATE INDEX [IX_ClothSalesReturns_BusinessId] ON [dbo].[ClothSalesReturns]([BusinessId]);
END
GO

-- 22. ClothSalesReturnItems
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ClothSalesReturnItems')
BEGIN
    CREATE TABLE [dbo].[ClothSalesReturnItems] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        [ClothSalesReturnId] UNIQUEIDENTIFIER NOT NULL,
        [ItemAction] NVARCHAR(50) NOT NULL DEFAULT 'Return',
        [ClothProductVariantId] UNIQUEIDENTIFIER NOT NULL,
        [ItemDescription] NVARCHAR(250) NOT NULL,
        [Sku] NVARCHAR(100) NOT NULL,
        [Barcode] NVARCHAR(100) NOT NULL,
        [SizeName] NVARCHAR(50) NULL,
        [ColourName] NVARCHAR(50) NULL,
        [Quantity] INT NOT NULL DEFAULT 1,
        [UnitPrice] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [LineTotal] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [Condition] NVARCHAR(100) NOT NULL DEFAULT 'Fresh / Resaleable',
        [CreatedOn] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        [CreatedBy] NVARCHAR(256) NULL,
        [UpdatedOn] DATETIMEOFFSET NULL,
        [UpdatedBy] NVARCHAR(256) NULL,
        [IsDeleted] BIT NOT NULL DEFAULT 0
    );
    CREATE INDEX [IX_ClothSalesReturnItems_ClothSalesReturnId] ON [dbo].[ClothSalesReturnItems]([ClothSalesReturnId]);
END
GO

-- 23. ClothCustomers
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ClothCustomers')
BEGIN
    CREATE TABLE [dbo].[ClothCustomers] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        [BusinessId] UNIQUEIDENTIFIER NOT NULL,
        [CustomerName] NVARCHAR(200) NOT NULL,
        [Phone] NVARCHAR(50) NULL,
        [Email] NVARCHAR(200) NULL,
        [Address] NVARCHAR(500) NULL,
        [City] NVARCHAR(100) NULL,
        [Gstin] NVARCHAR(50) NULL,
        [CreditLimit] DECIMAL(18,2) NOT NULL DEFAULT 5000,
        [CurrentOutstanding] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [TotalSpentAmount] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [TotalVisitsCount] INT NOT NULL DEFAULT 0,
        [DateOfBirth] DATETIME2 NULL,
        [AnniversaryDate] DATETIME2 NULL,
        [Notes] NVARCHAR(MAX) NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedOn] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        [CreatedBy] NVARCHAR(256) NULL,
        [UpdatedOn] DATETIMEOFFSET NULL,
        [UpdatedBy] NVARCHAR(256) NULL,
        [IsDeleted] BIT NOT NULL DEFAULT 0
    );
    CREATE INDEX [IX_ClothCustomers_BusinessId] ON [dbo].[ClothCustomers]([BusinessId]);
    CREATE INDEX [IX_ClothCustomers_Phone] ON [dbo].[ClothCustomers]([Phone]);
END
GO

-- 24. ClothExpenses
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ClothExpenses')
BEGIN
    CREATE TABLE [dbo].[ClothExpenses] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        [BusinessId] UNIQUEIDENTIFIER NOT NULL,
        [VoucherNumber] NVARCHAR(100) NOT NULL,
        [ExpenseDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [ExpenseCategory] NVARCHAR(100) NOT NULL DEFAULT 'Staff Welfare / Tea & Snacks',
        [Amount] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [PaymentMode] NVARCHAR(50) NOT NULL DEFAULT 'Cash',
        [PaidTo] NVARCHAR(200) NULL,
        [Notes] NVARCHAR(MAX) NULL,
        [ApprovedBy] NVARCHAR(200) NULL,
        [CreatedOn] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        [CreatedBy] NVARCHAR(256) NULL,
        [UpdatedOn] DATETIMEOFFSET NULL,
        [UpdatedBy] NVARCHAR(256) NULL,
        [IsDeleted] BIT NOT NULL DEFAULT 0
    );
    CREATE INDEX [IX_ClothExpenses_BusinessId] ON [dbo].[ClothExpenses]([BusinessId]);
END
GO

PRINT 'Cloth Hub schema created successfully!';
