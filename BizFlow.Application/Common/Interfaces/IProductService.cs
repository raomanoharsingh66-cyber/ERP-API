using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Inventory;

namespace BizFlow.Application.Common.Interfaces;

public interface IProductService
{
    Task<ApiResponse<PagedResult<ProductListItemDto>>> GetProductsAsync(ProductFilterDto filter, CancellationToken cancellationToken = default);
    Task<ApiResponse<ProductDto>> GetProductByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApiResponse<ProductDto>> GetProductBySkuAsync(string sku, CancellationToken cancellationToken = default);
    Task<ApiResponse<ProductDto>> CreateProductAsync(CreateProductDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse<ProductDto>> UpdateProductAsync(Guid id, UpdateProductDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse> DeleteProductAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApiResponse<List<ProductListItemDto>>> GetLowStockProductsAsync(CancellationToken cancellationToken = default);
}
