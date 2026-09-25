using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Sales;

namespace BizFlow.Application.Common.Interfaces;

public interface ICustomerService
{
    Task<ApiResponse<PagedResult<CustomerDto>>> GetCustomersAsync(
        string? search = null,
        bool? isActive = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<CustomerDto>> GetCustomerByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ApiResponse<CustomerDto>> CreateCustomerAsync(CreateCustomerDto dto, CancellationToken cancellationToken = default);

    Task<ApiResponse<CustomerDto>> UpdateCustomerAsync(Guid id, UpdateCustomerDto dto, CancellationToken cancellationToken = default);

    Task<ApiResponse<bool>> DeleteCustomerAsync(Guid id, CancellationToken cancellationToken = default);
}
