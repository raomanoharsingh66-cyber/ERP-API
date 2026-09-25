using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Sales;
using BizFlow.Domain.Enums;

namespace BizFlow.Application.Common.Interfaces;

public interface ISalesService
{
    // Sales Orders
    Task<ApiResponse<PagedResult<SalesOrderDto>>> GetSalesOrdersAsync(
        Guid? customerId = null,
        OrderStatus? status = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<SalesOrderDto>> GetSalesOrderByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ApiResponse<SalesOrderDto>> CreateSalesOrderAsync(CreateSalesOrderDto dto, CancellationToken cancellationToken = default);

    Task<ApiResponse<SalesOrderDto>> UpdateOrderStatusAsync(Guid id, UpdateOrderStatusDto dto, CancellationToken cancellationToken = default);

    // Sales Invoices
    Task<ApiResponse<PagedResult<SalesInvoiceDto>>> GetSalesInvoicesAsync(
        Guid? customerId = null,
        InvoiceStatus? status = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<SalesInvoiceDto>> GetSalesInvoiceByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ApiResponse<SalesInvoiceDto>> CreateSalesInvoiceAsync(CreateSalesInvoiceDto dto, CancellationToken cancellationToken = default);

    Task<ApiResponse<bool>> CancelSalesInvoiceAsync(Guid id, CancellationToken cancellationToken = default);

    // Payments
    Task<ApiResponse<SalesPaymentDto>> RecordPaymentAsync(RecordSalesPaymentDto dto, CancellationToken cancellationToken = default);

    // Financial Metrics
    Task<ApiResponse<SalesSummaryDto>> GetSalesSummaryAsync(CancellationToken cancellationToken = default);
}
