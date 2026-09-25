using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Inventory;

namespace BizFlow.Application.Common.Interfaces;

public interface IInventoryService
{
    Task<ApiResponse<List<InventoryStockDto>>> GetProductStocksAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<ApiResponse<List<InventoryStockDto>>> GetWarehouseStocksAsync(Guid warehouseId, CancellationToken cancellationToken = default);
    Task<ApiResponse<InventoryStockDto>> AdjustStockAsync(StockAdjustmentDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> TransferStockAsync(StockTransferDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse<PagedResult<StockTransactionDto>>> GetStockTransactionsAsync(Guid? productId, Guid? warehouseId, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default);
}
