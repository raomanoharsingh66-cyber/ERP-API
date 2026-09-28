using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Inventory;
using BizFlow.Domain.Entities;
using BizFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BizFlow.Infrastructure.Services;

public class ProductService : IProductService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ProductService(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ApiResponse<PagedResult<ProductListItemDto>>> GetProductsAsync(
        ProductFilterDto filter, 
        CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(1, filter.PageNumber);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);

        var query = _context.Products
            .Include(p => p.Category)
            .Include(p => p.UnitOfMeasure)
            .Include(p => p.Stocks)
            .AsNoTracking();

        if (filter.IsActiveOnly == true)
        {
            query = query.Where(p => p.IsActive);
        }

        if (filter.CategoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == filter.CategoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var s = filter.Search.Trim().ToLower();
            query = query.Where(p => 
                p.Name.ToLower().Contains(s) || 
                p.SKU.ToLower().Contains(s) || 
                (p.Barcode != null && p.Barcode.ToLower().Contains(s)));
        }

        if (filter.WarehouseId.HasValue)
        {
            query = query.Where(p => p.Stocks.Any(s => s.WarehouseId == filter.WarehouseId.Value));
        }

        if (filter.IsLowStockOnly == true)
        {
            query = query.Where(p => p.Stocks.Sum(s => s.QuantityOnHand) <= p.MinStockLevel);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(p => p.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductListItemDto
            {
                Id = p.Id,
                SKU = p.SKU,
                Name = p.Name,
                CategoryName = p.Category.Name,
                UnitOfMeasureCode = p.UnitOfMeasure.Code,
                PurchasePrice = p.PurchasePrice,
                SellingPrice = p.SellingPrice,
                TaxRate = p.TaxRate,
                MinStockLevel = p.MinStockLevel,
                TotalStockQuantity = p.Stocks.Sum(s => s.QuantityOnHand),
                TotalStockAvailable = p.Stocks.Sum(s => s.QuantityOnHand - s.QuantityReserved),
                IsActive = p.IsActive
            })
            .ToListAsync(cancellationToken);

        var paged = PagedResult<ProductListItemDto>.Create(items, totalCount, pageNumber, pageSize);
        return ApiResponse<PagedResult<ProductListItemDto>>.SuccessResult(paged);
    }

    public async Task<ApiResponse<ProductDto>> GetProductByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var p = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.UnitOfMeasure)
            .Include(p => p.Stocks)
                .ThenInclude(s => s.Warehouse)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (p == null)
        {
            throw new NotFoundException("Product", id);
        }

        var dto = new ProductDto
        {
            Id = p.Id,
            CategoryId = p.CategoryId,
            CategoryName = p.Category.Name,
            UnitOfMeasureId = p.UnitOfMeasureId,
            UnitOfMeasureCode = p.UnitOfMeasure.Code,
            SKU = p.SKU,
            Barcode = p.Barcode,
            Name = p.Name,
            Description = p.Description,
            PurchasePrice = p.PurchasePrice,
            SellingPrice = p.SellingPrice,
            TaxRate = p.TaxRate,
            HSNCode = p.HSNCode,
            MinStockLevel = p.MinStockLevel,
            MaxStockLevel = p.MaxStockLevel,
            IsActive = p.IsActive,
            TotalStockQuantity = p.Stocks.Sum(s => s.QuantityOnHand),
            TotalStockAvailable = p.Stocks.Sum(s => s.QuantityOnHand - s.QuantityReserved),
            WarehouseStocks = p.Stocks.Select(s => new InventoryStockDto
            {
                Id = s.Id,
                ProductId = s.ProductId,
                ProductSKU = p.SKU,
                ProductName = p.Name,
                UnitOfMeasureCode = p.UnitOfMeasure.Code,
                WarehouseId = s.WarehouseId,
                WarehouseName = s.Warehouse.Name,
                WarehouseCode = s.Warehouse.Code,
                QuantityOnHand = s.QuantityOnHand,
                QuantityReserved = s.QuantityReserved,
                QuantityAvailable = s.QuantityOnHand - s.QuantityReserved,
                AverageCost = s.AverageCost
            }).ToList()
        };

        return ApiResponse<ProductDto>.SuccessResult(dto);
    }

    public async Task<ApiResponse<ProductDto>> GetProductBySkuAsync(string sku, CancellationToken cancellationToken = default)
    {
        var normalizedSku = sku.Trim().ToUpperInvariant();
        var product = await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.SKU == normalizedSku, cancellationToken);

        if (product == null)
        {
            throw new NotFoundException("Product with SKU", normalizedSku);
        }

        return await GetProductByIdAsync(product.Id, cancellationToken);
    }

    public async Task<ApiResponse<ProductDto>> CreateProductAsync(CreateProductDto dto, CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.BusinessId;
        if (businessId == null)
        {
            var firstBiz = await _context.Businesses.IgnoreQueryFilters().FirstOrDefaultAsync(cancellationToken);
            businessId = firstBiz?.Id ?? Guid.NewGuid();
        }

        if (dto.CategoryId == Guid.Empty)
        {
            var firstCat = await _context.Categories.IgnoreQueryFilters().FirstOrDefaultAsync(cancellationToken);
            if (firstCat != null) dto.CategoryId = firstCat.Id;
        }

        if (dto.UnitOfMeasureId == Guid.Empty)
        {
            var firstUom = await _context.UnitsOfMeasure.IgnoreQueryFilters().FirstOrDefaultAsync(cancellationToken);
            if (firstUom != null) dto.UnitOfMeasureId = firstUom.Id;
        }

        var normalizedSku = dto.SKU.Trim().ToUpperInvariant();

        var skuExists = await _context.Products
            .AnyAsync(p => p.SKU == normalizedSku, cancellationToken);

        if (skuExists)
        {
            throw new ValidationException(new List<string> { $"A product with SKU '{normalizedSku}' already exists in your inventory." });
        }

        var product = new Product
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId.Value,
            CategoryId = dto.CategoryId,
            UnitOfMeasureId = dto.UnitOfMeasureId,
            SKU = normalizedSku,
            Barcode = dto.Barcode?.Trim(),
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim(),
            PurchasePrice = dto.PurchasePrice,
            SellingPrice = dto.SellingPrice,
            TaxRate = dto.TaxRate,
            HSNCode = dto.HSNCode?.Trim(),
            MinStockLevel = dto.MinStockLevel,
            MaxStockLevel = dto.MaxStockLevel,
            IsActive = true,
            CreatedOn = _dateTimeProvider.UtcNow,
            CreatedBy = _currentUserService.Email ?? "System"
        };

        _context.Products.Add(product);

        // Process optional initial stock
        if (dto.InitialQuantity > 0 && dto.InitialWarehouseId.HasValue)
        {
            var stock = new InventoryStock
            {
                Id = Guid.NewGuid(),
                BusinessId = businessId.Value,
                ProductId = product.Id,
                WarehouseId = dto.InitialWarehouseId.Value,
                QuantityOnHand = dto.InitialQuantity,
                QuantityReserved = 0m,
                AverageCost = dto.PurchasePrice,
                CreatedOn = _dateTimeProvider.UtcNow,
                CreatedBy = _currentUserService.Email ?? "System"
            };

            _context.InventoryStocks.Add(stock);

            // Record transaction ledger
            _context.StockTransactions.Add(new StockTransaction
            {
                Id = Guid.NewGuid(),
                BusinessId = businessId.Value,
                ProductId = product.Id,
                WarehouseId = dto.InitialWarehouseId.Value,
                TransactionType = StockTransactionType.Inward,
                Quantity = dto.InitialQuantity,
                UnitPrice = dto.PurchasePrice,
                TotalAmount = dto.InitialQuantity * dto.PurchasePrice,
                ReferenceType = "InitialStock",
                ReferenceId = "PRODUCT-INIT",
                Notes = "Initial stock entry on product registration",
                Timestamp = _dateTimeProvider.UtcNow,
                CreatedOn = _dateTimeProvider.UtcNow,
                CreatedBy = _currentUserService.Email ?? "System"
            });
        }

        await _context.SaveChangesAsync(cancellationToken);

        return await GetProductByIdAsync(product.Id, cancellationToken);
    }

    public async Task<ApiResponse<ProductDto>> UpdateProductAsync(Guid id, UpdateProductDto dto, CancellationToken cancellationToken = default)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (product == null)
        {
            throw new NotFoundException("Product", id);
        }

        product.CategoryId = dto.CategoryId;
        product.UnitOfMeasureId = dto.UnitOfMeasureId;
        product.Barcode = dto.Barcode?.Trim();
        product.Name = dto.Name.Trim();
        product.Description = dto.Description?.Trim();
        product.PurchasePrice = dto.PurchasePrice;
        product.SellingPrice = dto.SellingPrice;
        product.TaxRate = dto.TaxRate;
        product.HSNCode = dto.HSNCode?.Trim();
        product.MinStockLevel = dto.MinStockLevel;
        product.MaxStockLevel = dto.MaxStockLevel;
        product.IsActive = dto.IsActive;
        product.UpdatedOn = _dateTimeProvider.UtcNow;
        product.UpdatedBy = _currentUserService.Email ?? "System";

        await _context.SaveChangesAsync(cancellationToken);

        return await GetProductByIdAsync(product.Id, cancellationToken);
    }

    public async Task<ApiResponse> DeleteProductAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (product == null)
        {
            throw new NotFoundException("Product", id);
        }

        // Check if there is active physical stock
        var hasActiveStock = await _context.InventoryStocks
            .AnyAsync(s => s.ProductId == id && s.QuantityOnHand > 0, cancellationToken);

        if (hasActiveStock)
        {
            throw new BusinessRuleException("Cannot delete product with existing stock on hand. Adjust stock to zero first.");
        }

        _context.Products.Remove(product); // triggers soft delete
        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse.SuccessResult("Product removed successfully.");
    }

    public async Task<ApiResponse<List<ProductListItemDto>>> GetLowStockProductsAsync(CancellationToken cancellationToken = default)
    {
        var lowStockItems = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.UnitOfMeasure)
            .Include(p => p.Stocks)
            .AsNoTracking()
            .Where(p => p.IsActive && p.Stocks.Sum(s => s.QuantityOnHand) <= p.MinStockLevel)
            .Select(p => new ProductListItemDto
            {
                Id = p.Id,
                SKU = p.SKU,
                Name = p.Name,
                CategoryName = p.Category.Name,
                UnitOfMeasureCode = p.UnitOfMeasure.Code,
                PurchasePrice = p.PurchasePrice,
                SellingPrice = p.SellingPrice,
                TaxRate = p.TaxRate,
                MinStockLevel = p.MinStockLevel,
                TotalStockQuantity = p.Stocks.Sum(s => s.QuantityOnHand),
                TotalStockAvailable = p.Stocks.Sum(s => s.QuantityOnHand - s.QuantityReserved),
                IsActive = p.IsActive
            })
            .ToListAsync(cancellationToken);

        return ApiResponse<List<ProductListItemDto>>.SuccessResult(lowStockItems);
    }
}
