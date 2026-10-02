using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BizFlow.Application.DTOs.ClothHub;

namespace BizFlow.Application.Common.Interfaces;

public interface IClothMasterService
{
    Task<List<ClothBrandDto>> GetBrandsAsync(CancellationToken cancellationToken = default);
    Task<ClothBrandDto> CreateBrandAsync(CreateClothBrandDto dto, CancellationToken cancellationToken = default);

    Task<List<ClothCategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<ClothCategoryDto> CreateCategoryAsync(CreateClothCategoryDto dto, CancellationToken cancellationToken = default);

    Task<List<ClothSizeDto>> GetSizesAsync(CancellationToken cancellationToken = default);
    Task<ClothSizeDto> CreateSizeAsync(CreateClothSizeDto dto, CancellationToken cancellationToken = default);

    Task<List<ClothColourDto>> GetColoursAsync(CancellationToken cancellationToken = default);
    Task<ClothColourDto> CreateColourAsync(CreateClothColourDto dto, CancellationToken cancellationToken = default);

    Task<List<ClothFabricDto>> GetFabricsAsync(CancellationToken cancellationToken = default);
    Task<ClothFabricDto> CreateFabricAsync(CreateClothFabricDto dto, CancellationToken cancellationToken = default);
}

public interface IClothProductService
{
    Task<List<ClothProductDetailDto>> GetProductsAsync(string? search = null, Guid? brandId = null, Guid? categoryId = null, CancellationToken cancellationToken = default);
    Task<ClothProductDetailDto?> GetProductByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ClothProductDetailDto> CreateProductWithVariantsAsync(CreateClothProductRequestDto dto, CancellationToken cancellationToken = default);
    Task<bool> UpdateVariantStockAsync(Guid variantId, int newStock, CancellationToken cancellationToken = default);
}

public interface IClothInventoryService
{
    Task<ClothStockMatrixDto?> GetStockMatrixAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<ClothInventorySummaryDto> GetInventorySummaryAsync(CancellationToken cancellationToken = default);
    
    // Box / Pack Assortments
    Task<List<ClothBoxPackDto>> GetBoxPacksAsync(CancellationToken cancellationToken = default);
    Task<ClothBoxPackDto> CreateBoxPackAsync(CreateClothBoxPackDto dto, CancellationToken cancellationToken = default);
    Task<bool> UnpackBoxAsync(Guid boxPackId, int boxesToUnpack, string? remarks = null, CancellationToken cancellationToken = default);
    Task<bool> PackBoxAsync(Guid boxPackId, int boxesToPack, string? remarks = null, CancellationToken cancellationToken = default);

    // Stock Adjustments
    Task<List<ClothStockAdjustmentDto>> GetAdjustmentsAsync(CancellationToken cancellationToken = default);
    Task<ClothStockAdjustmentDto> CreateAdjustmentAsync(CreateClothStockAdjustmentDto dto, CancellationToken cancellationToken = default);

    // Stock Ledger
    Task<List<ClothStockLedgerDto>> GetStockLedgerAsync(Guid? variantId = null, string? transactionType = null, CancellationToken cancellationToken = default);
}

public interface IClothPurchaseService
{
    // Suppliers
    Task<List<ClothSupplierDto>> GetSuppliersAsync(CancellationToken cancellationToken = default);
    Task<ClothSupplierDto> CreateSupplierAsync(CreateClothSupplierDto dto, CancellationToken cancellationToken = default);

    // Purchase Bills & Goods Inward
    Task<List<ClothPurchaseBillDto>> GetPurchaseBillsAsync(CancellationToken cancellationToken = default);
    Task<ClothPurchaseBillDto?> GetPurchaseBillByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ClothPurchaseBillDto> CreatePurchaseBillAsync(CreateClothPurchaseBillDto dto, CancellationToken cancellationToken = default);
    Task<bool> RecordSupplierPaymentAsync(Guid billId, RecordSupplierPaymentDto dto, CancellationToken cancellationToken = default);

    // Purchase Returns & Debit Notes
    Task<List<ClothPurchaseReturnDto>> GetPurchaseReturnsAsync(CancellationToken cancellationToken = default);
    Task<ClothPurchaseReturnDto> CreatePurchaseReturnAsync(CreateClothPurchaseReturnDto dto, CancellationToken cancellationToken = default);

    // Summary
    Task<ClothPurchaseSummaryDto> GetPurchaseSummaryAsync(CancellationToken cancellationToken = default);
}

public interface IClothSalesService
{
    // POS Fast Lookup
    Task<ClothPosLookupItemDto?> LookupVariantByBarcodeOrSkuAsync(string term, CancellationToken cancellationToken = default);
    Task<List<ClothPosLookupItemDto>> SearchPosItemsAsync(string? query = null, Guid? categoryId = null, CancellationToken cancellationToken = default);

    // Invoices & Billing
    Task<List<ClothSalesInvoiceDto>> GetSalesInvoicesAsync(DateTime? fromDate = null, DateTime? toDate = null, string? search = null, CancellationToken cancellationToken = default);
    Task<ClothSalesInvoiceDto?> GetSalesInvoiceByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ClothSalesInvoiceDto> CreateSalesInvoiceAsync(CreateClothSalesInvoiceDto dto, CancellationToken cancellationToken = default);

    // Summary
    Task<ClothSalesSummaryDto> GetSalesSummaryAsync(CancellationToken cancellationToken = default);
}

public interface IClothSalesReturnService
{
    Task<List<ClothSalesReturnDto>> GetReturnsAsync(DateTime? fromDate = null, DateTime? toDate = null, string? search = null, string? returnType = null, CancellationToken cancellationToken = default);
    Task<ClothSalesReturnDto?> GetReturnByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ClothSalesReturnDto> CreateReturnOrExchangeAsync(CreateClothSalesReturnDto dto, CancellationToken cancellationToken = default);
    Task<ClothSalesReturnSummaryDto> GetReturnsSummaryAsync(CancellationToken cancellationToken = default);
}

public interface IClothAnalyticsService
{
    Task<ClothDailySalesReportDto> GetDailySalesReportAsync(DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
    Task<ClothProfitMarginReportDto> GetProfitAndMarginReportAsync(DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
    Task<ClothSizeColourPerformanceReportDto> GetSizeColourPerformanceReportAsync(DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
    Task<ClothApparelGstReportDto> GetApparelGstReportAsync(DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
}

public interface IClothCustomerService
{
    Task<List<ClothCustomerDto>> GetCustomersAsync(string? search = null, CancellationToken cancellationToken = default);
    Task<ClothCustomerDto?> GetCustomerByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ClothCustomerDto> CreateCustomerAsync(CreateClothCustomerDto dto, CancellationToken cancellationToken = default);
    Task<ClothCustomerDto> SettleCreditAsync(SettleCustomerCreditDto dto, CancellationToken cancellationToken = default);
}

public interface IClothExpenseService
{
    Task<List<ClothExpenseDto>> GetExpensesAsync(DateTime? fromDate = null, DateTime? toDate = null, string? category = null, CancellationToken cancellationToken = default);
    Task<ClothExpenseDto> CreateExpenseAsync(CreateClothExpenseDto dto, CancellationToken cancellationToken = default);
    Task<ClothExpenseSummaryDto> GetExpenseSummaryAsync(CancellationToken cancellationToken = default);
}



