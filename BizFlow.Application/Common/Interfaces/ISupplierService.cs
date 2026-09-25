using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Purchases;

namespace BizFlow.Application.Common.Interfaces;

public interface ISupplierService
{
    Task<ApiResponse<PagedResult<SupplierDto>>> GetSuppliersAsync(
        string? search = null,
        bool? isActive = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<SupplierDto>> GetSupplierByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ApiResponse<SupplierDto>> CreateSupplierAsync(CreateSupplierDto dto, CancellationToken cancellationToken = default);

    Task<ApiResponse<SupplierDto>> UpdateSupplierAsync(Guid id, UpdateSupplierDto dto, CancellationToken cancellationToken = default);

    Task<ApiResponse<bool>> DeleteSupplierAsync(Guid id, CancellationToken cancellationToken = default);
}
