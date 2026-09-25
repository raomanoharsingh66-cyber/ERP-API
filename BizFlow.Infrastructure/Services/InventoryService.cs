using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Inventory;
using BizFlow.Domain.Entities;
using BizFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BizFlow.Infrastructure.Services;

public class InventoryService : IInventoryService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public InventoryService(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ApiResponse<List<InventoryStockDto>>> GetProductStocksAsync(
        Guid productId, 
        CancellationToken cancellationToken = default)
    {
        var stocks = await _context.InventoryStocks
            .Include(s => s.Product)
                .ThenInclude(p => p.UnitOfMeasure)
            .Include(s => s.Warehouse)
            .AsNoTracking()
            .Where(s => s.ProductId == productId)
            .Select(s => new InventoryStockDto
            {
                Id = s.Id,
                ProductId = s.ProductId,
                ProductSKU = s.Product.SKU,
                ProductName = s.Product.Name,
                UnitOfMeasureCode = s.Product.UnitOfMeasure.Code,
                WarehouseId = s.WarehouseId,
                WarehouseName = s.Warehouse.Name,
                WarehouseCode = s.Warehouse.Code,
                QuantityOnHand = s.QuantityOnHand,
                QuantityReserved = s.QuantityReserved,
                QuantityAvailable = s.QuantityOnHand - s.QuantityReserved,
                AverageCost = s.AverageCost
            })
            .ToListAsync(cancellationToken);

        return ApiResponse<List<InventoryStockDto>>.SuccessResult(stocks);
    }

    public async Task<ApiResponse<List<InventoryStockDto>>> GetWarehouseStocksAsync(
        Guid warehouseId, 
        CancellationToken cancellationToken = default)
    {
        var stocks = await _context.InventoryStocks
            .Include(s => s.Product)
                .ThenInclude(p => p.UnitOfMeasure)
            .Include(s => s.Warehouse)
            .AsNoTracking()
            .Where(s => s.WarehouseId == warehouseId)
            .Select(s => new InventoryStockDto
            {
                Id = s.Id,
                ProductId = s.ProductId,
                ProductSKU = s.Product.SKU,
                ProductName = s.Product.Name,
                UnitOfMeasureCode = s.Product.UnitOfMeasure.Code,
                WarehouseId = s.WarehouseId,
                WarehouseName = s.Warehouse.Name,
                WarehouseCode = s.Warehouse.Code,
                QuantityOnHand = s.QuantityOnHand,
                QuantityReserved = s.QuantityReserved,
                QuantityAvailable = s.QuantityOnHand - s.QuantityReserved,
                AverageCost = s.AverageCost
            })
            .ToListAsync(cancellationToken);

        return ApiResponse<List<InventoryStockDto>>.SuccessResult(stocks);
    }

    public async Task<ApiResponse<InventoryStockDto>> AdjustStockAsync(
        StockAdjustmentDto dto, 
        CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.BusinessId;
        if (!_currentUserService.IsSuperAdmin && businessId == null)
        {
            throw new BusinessRuleException("No active business tenant context in current session.");
        }

        var product = await _context.Products
            .Include(p => p.UnitOfMeasure)
            .FirstOrDefaultAsync(p => p.Id == dto.ProductId, cancellationToken);

        if (product == null)
        {
            throw new NotFoundException("Product", dto.ProductId);
        }

        var warehouse = await _context.Warehouses
            .FirstOrDefaultAsync(w => w.Id == dto.WarehouseId, cancellationToken);

        if (warehouse == null)
        {
            throw new NotFoundException("Warehouse", dto.WarehouseId);
        }

        var stock = await _context.InventoryStocks
            .FirstOrDefaultAsync(s => s.ProductId == dto.ProductId && s.WarehouseId == dto.WarehouseId, cancellationToken);

        var now = _dateTimeProvider.UtcNow;
        var userEmail = _currentUserService.Email ?? "System";
        decimal quantityChanged = 0;
        var unitPrice = dto.UnitPrice ?? product.PurchasePrice;

        if (stock == null)
        {
            stock = new InventoryStock
            {
                Id = Guid.NewGuid(),
                BusinessId = product.BusinessId,
                ProductId = product.Id,
                WarehouseId = warehouse.Id,
                QuantityOnHand = 0m,
                QuantityReserved = 0m,
                AverageCost = unitPrice,
                CreatedOn = now,
                CreatedBy = userEmail
            };
            _context.InventoryStocks.Add(stock);
        }

        switch (dto.Type)
        {
            case StockTransactionType.Inward:
                if (dto.Quantity <= 0)
                {
                    throw new BusinessRuleException("Inward quantity must be positive.");
                }

                // Recalculate weighted average cost
                var currentTotalValue = stock.QuantityOnHand * stock.AverageCost;
                var newInwardValue = dto.Quantity * unitPrice;
                var newTotalQty = stock.QuantityOnHand + dto.Quantity;

                stock.AverageCost = newTotalQty > 0 ? (currentTotalValue + newInwardValue) / newTotalQty : unitPrice;
                stock.QuantityOnHand += dto.Quantity;
                quantityChanged = dto.Quantity;
                break;

            case StockTransactionType.Outward:
                if (dto.Quantity <= 0)
                {
                    throw new BusinessRuleException("Outward quantity must be positive.");
                }

                if (stock.QuantityAvailable < dto.Quantity)
                {
                    throw new BusinessRuleException($"Insufficient available stock. Current available: {stock.QuantityAvailable} {product.UnitOfMeasure.Code}, requested: {dto.Quantity}");
                }

                stock.QuantityOnHand -= dto.Quantity;
                quantityChanged = -dto.Quantity;
                break;

            case StockTransactionType.Adjustment:
                // Set absolute count
                var diff = dto.Quantity - stock.QuantityOnHand;
                if (diff < 0 && stock.QuantityAvailable < Math.Abs(diff))
                {
                    throw new BusinessRuleException($"Cannot adjust stock below reserved quantity. Available: {stock.QuantityAvailable}");
                }

                quantityChanged = diff;
                stock.QuantityOnHand = dto.Quantity;
                break;
        }

        stock.UpdatedOn = now;
        stock.UpdatedBy = userEmail;

        // Record immutable transaction log
        var transaction = new StockTransaction
        {
            Id = Guid.NewGuid(),
            BusinessId = product.BusinessId,
            ProductId = product.Id,
            WarehouseId = warehouse.Id,
            TransactionType = dto.Type,
            Quantity = Math.Abs(quantityChanged),
            UnitPrice = unitPrice,
            TotalAmount = Math.Abs(quantityChanged) * unitPrice,
            ReferenceType = "Adjustment",
            ReferenceId = dto.Reason ?? "MANUAL-ADJUSTMENT",
            Notes = dto.Notes,
            Timestamp = now,
            CreatedOn = now,
            CreatedBy = userEmail
        };

        _context.StockTransactions.Add(transaction);

        await _context.SaveChangesAsync(cancellationToken);

        var resultDto = new InventoryStockDto
        {
            Id = stock.Id,
            ProductId = product.Id,
            ProductSKU = product.SKU,
            ProductName = product.Name,
            UnitOfMeasureCode = product.UnitOfMeasure.Code,
            WarehouseId = warehouse.Id,
            WarehouseName = warehouse.Name,
            WarehouseCode = warehouse.Code,
            QuantityOnHand = stock.QuantityOnHand,
            QuantityReserved = stock.QuantityReserved,
            QuantityAvailable = stock.QuantityOnHand - stock.QuantityReserved,
            AverageCost = stock.AverageCost
        };

        return ApiResponse<InventoryStockDto>.SuccessResult(resultDto, "Stock adjusted successfully.");
    }

    public async Task<ApiResponse<bool>> TransferStockAsync(
        StockTransferDto dto, 
        CancellationToken cancellationToken = default)
    {
        if (dto.FromWarehouseId == dto.ToWarehouseId)
        {
            throw new BusinessRuleException("Source and destination warehouses cannot be the same.");
        }

        if (dto.Quantity <= 0)
        {
            throw new BusinessRuleException("Transfer quantity must be greater than zero.");
        }

        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == dto.ProductId, cancellationToken);
        if (product == null)
        {
            throw new NotFoundException("Product", dto.ProductId);
        }

        var fromStock = await _context.InventoryStocks
            .FirstOrDefaultAsync(s => s.ProductId == dto.ProductId && s.WarehouseId == dto.FromWarehouseId, cancellationToken);

        if (fromStock == null || fromStock.QuantityAvailable < dto.Quantity)
        {
            throw new BusinessRuleException($"Insufficient stock in source warehouse. Available: {fromStock?.QuantityAvailable ?? 0}");
        }

        var toStock = await _context.InventoryStocks
            .FirstOrDefaultAsync(s => s.ProductId == dto.ProductId && s.WarehouseId == dto.ToWarehouseId, cancellationToken);

        var now = _dateTimeProvider.UtcNow;
        var userEmail = _currentUserService.Email ?? "System";
        var transferRefId = $"TRF-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant()}";

        // Deduct from source warehouse
        fromStock.QuantityOnHand -= dto.Quantity;
        fromStock.UpdatedOn = now;
        fromStock.UpdatedBy = userEmail;

        _context.StockTransactions.Add(new StockTransaction
        {
            Id = Guid.NewGuid(),
            BusinessId = product.BusinessId,
            ProductId = product.Id,
            WarehouseId = dto.FromWarehouseId,
            TransactionType = StockTransactionType.TransferOut,
            Quantity = dto.Quantity,
            UnitPrice = fromStock.AverageCost,
            TotalAmount = dto.Quantity * fromStock.AverageCost,
            ReferenceType = "Transfer",
            ReferenceId = transferRefId,
            Notes = $"Transferred to warehouse ID: {dto.ToWarehouseId}. {dto.Notes}",
            Timestamp = now,
            CreatedOn = now,
            CreatedBy = userEmail
        });

        // Add to destination warehouse
        if (toStock == null)
        {
            toStock = new InventoryStock
            {
                Id = Guid.NewGuid(),
                BusinessId = product.BusinessId,
                ProductId = product.Id,
                WarehouseId = dto.ToWarehouseId,
                QuantityOnHand = dto.Quantity,
                QuantityReserved = 0m,
                AverageCost = fromStock.AverageCost,
                CreatedOn = now,
                CreatedBy = userEmail
            };
            _context.InventoryStocks.Add(toStock);
        }
        else
        {
            var totalValue = (toStock.QuantityOnHand * toStock.AverageCost) + (dto.Quantity * fromStock.AverageCost);
            var totalQty = toStock.QuantityOnHand + dto.Quantity;
            toStock.AverageCost = totalQty > 0 ? totalValue / totalQty : fromStock.AverageCost;
            toStock.QuantityOnHand += dto.Quantity;
            toStock.UpdatedOn = now;
            toStock.UpdatedBy = userEmail;
        }

        _context.StockTransactions.Add(new StockTransaction
        {
            Id = Guid.NewGuid(),
            BusinessId = product.BusinessId,
            ProductId = product.Id,
            WarehouseId = dto.ToWarehouseId,
            TransactionType = StockTransactionType.TransferIn,
            Quantity = dto.Quantity,
            UnitPrice = fromStock.AverageCost,
            TotalAmount = dto.Quantity * fromStock.AverageCost,
            ReferenceType = "Transfer",
            ReferenceId = transferRefId,
            Notes = $"Transferred from warehouse ID: {dto.FromWarehouseId}. {dto.Notes}",
            Timestamp = now,
            CreatedOn = now,
            CreatedBy = userEmail
        });

        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.SuccessResult(true, "Stock transferred successfully.");
    }

    public async Task<ApiResponse<PagedResult<StockTransactionDto>>> GetStockTransactionsAsync(
        Guid? productId, 
        Guid? warehouseId, 
        int pageNumber = 1, 
        int pageSize = 20, 
        CancellationToken cancellationToken = default)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.StockTransactions
            .Include(t => t.Product)
            .Include(t => t.Warehouse)
            .AsNoTracking();

        if (productId.HasValue)
        {
            query = query.Where(t => t.ProductId == productId.Value);
        }

        if (warehouseId.HasValue)
        {
            query = query.Where(t => t.WarehouseId == warehouseId.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(t => t.Timestamp)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new StockTransactionDto
            {
                Id = t.Id,
                ProductId = t.ProductId,
                ProductSKU = t.Product.SKU,
                ProductName = t.Product.Name,
                WarehouseId = t.WarehouseId,
                WarehouseName = t.Warehouse.Name,
                TransactionType = t.TransactionType.ToString(),
                Quantity = t.Quantity,
                UnitPrice = t.UnitPrice,
                TotalAmount = t.TotalAmount,
                ReferenceType = t.ReferenceType,
                ReferenceId = t.ReferenceId,
                Notes = t.Notes,
                Timestamp = t.Timestamp
            })
            .ToListAsync(cancellationToken);

        var paged = PagedResult<StockTransactionDto>.Create(items, totalCount, pageNumber, pageSize);
        return ApiResponse<PagedResult<StockTransactionDto>>.SuccessResult(paged);
    }
}
