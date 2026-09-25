using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Purchases;
using BizFlow.Domain.Enums;

namespace BizFlow.Application.Common.Interfaces;

public interface IPurchaseService
{
    // Purchase Orders
    Task<ApiResponse<PagedResult<PurchaseOrderDto>>> GetPurchaseOrdersAsync(
        Guid? supplierId = null,
        PurchaseOrderStatus? status = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<PurchaseOrderDto>> GetPurchaseOrderByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ApiResponse<PurchaseOrderDto>> CreatePurchaseOrderAsync(CreatePurchaseOrderDto dto, CancellationToken cancellationToken = default);

    Task<ApiResponse<PurchaseOrderDto>> UpdatePurchaseOrderStatusAsync(Guid id, UpdatePurchaseOrderStatusDto dto, CancellationToken cancellationToken = default);

    // Goods Receipt Notes (GRN)
    Task<ApiResponse<PagedResult<GoodsReceiptNoteDto>>> GetGoodsReceiptNotesAsync(
        Guid? purchaseOrderId = null,
        Guid? supplierId = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<GoodsReceiptNoteDto>> GetGoodsReceiptNoteByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ApiResponse<GoodsReceiptNoteDto>> CreateGoodsReceiptNoteAsync(CreateGoodsReceiptNoteDto dto, CancellationToken cancellationToken = default);

    // Purchase Bills
    Task<ApiResponse<PagedResult<PurchaseBillDto>>> GetPurchaseBillsAsync(
        Guid? supplierId = null,
        BillStatus? status = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<PurchaseBillDto>> GetPurchaseBillByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ApiResponse<PurchaseBillDto>> CreatePurchaseBillAsync(CreatePurchaseBillDto dto, CancellationToken cancellationToken = default);

    Task<ApiResponse<bool>> CancelPurchaseBillAsync(Guid id, CancellationToken cancellationToken = default);

    // Vendor Disbursements
    Task<ApiResponse<VendorPaymentDto>> RecordVendorPaymentAsync(RecordVendorPaymentDto dto, CancellationToken cancellationToken = default);

    // Procurement KPI Metrics
    Task<ApiResponse<PurchaseSummaryDto>> GetPurchaseSummaryAsync(CancellationToken cancellationToken = default);
}
