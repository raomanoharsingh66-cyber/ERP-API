using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Purchases;
using BizFlow.Domain.Entities;
using BizFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BizFlow.Infrastructure.Services;

public class PurchaseService : IPurchaseService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public PurchaseService(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    // ==========================================
    // PURCHASE ORDERS
    // ==========================================

    public async Task<ApiResponse<PagedResult<PurchaseOrderDto>>> GetPurchaseOrdersAsync(
        Guid? supplierId = null,
        PurchaseOrderStatus? status = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.PurchaseOrders
            .Include(p => p.Supplier)
            .Include(p => p.Warehouse)
            .Include(p => p.Items)
                .ThenInclude(i => i.Product)
                    .ThenInclude(pr => pr!.UnitOfMeasure)
            .AsNoTracking();

        if (supplierId.HasValue)
        {
            query = query.Where(p => p.SupplierId == supplierId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(p => p.Status == status.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(p => p.OrderDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PurchaseOrderDto
            {
                Id = p.Id,
                PONumber = p.PONumber,
                OrderDate = p.OrderDate,
                ExpectedDeliveryDate = p.ExpectedDeliveryDate,
                SupplierId = p.SupplierId,
                SupplierName = p.Supplier != null ? p.Supplier.Name : string.Empty,
                SupplierCode = p.Supplier != null ? p.Supplier.SupplierCode : string.Empty,
                WarehouseId = p.WarehouseId,
                WarehouseName = p.Warehouse != null ? p.Warehouse.Name : string.Empty,
                Status = p.Status,
                SubTotal = p.SubTotal,
                TaxAmount = p.TaxAmount,
                TotalAmount = p.TotalAmount,
                Notes = p.Notes,
                CreatedOn = p.CreatedOn,
                Items = p.Items.Select(i => new PurchaseOrderItemDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    ProductSKU = i.Product != null ? i.Product.SKU : string.Empty,
                    ProductName = i.Product != null ? i.Product.Name : string.Empty,
                    UnitOfMeasure = (i.Product != null && i.Product.UnitOfMeasure != null) ? i.Product.UnitOfMeasure.Code : string.Empty,
                    OrderedQuantity = i.OrderedQuantity,
                    ReceivedQuantity = i.ReceivedQuantity,
                    UnitPrice = i.UnitPrice,
                    TaxRate = i.TaxRate,
                    TaxAmount = i.TaxAmount,
                    TotalAmount = i.TotalAmount,
                    Notes = i.Notes
                }).ToList()
            })
            .ToListAsync(cancellationToken);

        var result = PagedResult<PurchaseOrderDto>.Create(items, totalCount, pageNumber, pageSize);
        return ApiResponse<PagedResult<PurchaseOrderDto>>.SuccessResult(result);
    }

    public async Task<ApiResponse<PurchaseOrderDto>> GetPurchaseOrderByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var p = await _context.PurchaseOrders
            .Include(x => x.Supplier)
            .Include(x => x.Warehouse)
            .Include(x => x.Items)
                .ThenInclude(i => i.Product)
                    .ThenInclude(pr => pr!.UnitOfMeasure)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (p == null)
        {
            throw new NotFoundException("PurchaseOrder", id);
        }

        var dto = new PurchaseOrderDto
        {
            Id = p.Id,
            PONumber = p.PONumber,
            OrderDate = p.OrderDate,
            ExpectedDeliveryDate = p.ExpectedDeliveryDate,
            SupplierId = p.SupplierId,
            SupplierName = p.Supplier?.Name ?? string.Empty,
            SupplierCode = p.Supplier?.SupplierCode ?? string.Empty,
            WarehouseId = p.WarehouseId,
            WarehouseName = p.Warehouse?.Name ?? string.Empty,
            Status = p.Status,
            SubTotal = p.SubTotal,
            TaxAmount = p.TaxAmount,
            TotalAmount = p.TotalAmount,
            Notes = p.Notes,
            CreatedOn = p.CreatedOn,
            Items = p.Items.Select(i => new PurchaseOrderItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductSKU = i.Product?.SKU ?? string.Empty,
                ProductName = i.Product?.Name ?? string.Empty,
                UnitOfMeasure = i.Product?.UnitOfMeasure?.Code ?? string.Empty,
                OrderedQuantity = i.OrderedQuantity,
                ReceivedQuantity = i.ReceivedQuantity,
                UnitPrice = i.UnitPrice,
                TaxRate = i.TaxRate,
                TaxAmount = i.TaxAmount,
                TotalAmount = i.TotalAmount,
                Notes = i.Notes
            }).ToList()
        };

        return ApiResponse<PurchaseOrderDto>.SuccessResult(dto);
    }

    public async Task<ApiResponse<PurchaseOrderDto>> CreatePurchaseOrderAsync(CreatePurchaseOrderDto dto, CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.BusinessId;
        if (!_currentUserService.IsSuperAdmin && businessId == null)
        {
            throw new BusinessRuleException("No active business tenant context in current session.");
        }

        var targetBusinessId = businessId ?? (await _context.Businesses.Select(b => b.Id).FirstOrDefaultAsync(cancellationToken));

        var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.Id == dto.SupplierId, cancellationToken);
        if (supplier == null)
        {
            throw new NotFoundException("Supplier", dto.SupplierId);
        }

        var warehouse = await _context.Warehouses.FirstOrDefaultAsync(w => w.Id == dto.WarehouseId, cancellationToken);
        if (warehouse == null)
        {
            throw new NotFoundException("Warehouse", dto.WarehouseId);
        }

        var count = await _context.PurchaseOrders.CountAsync(p => p.BusinessId == targetBusinessId, cancellationToken);
        var poNumber = $"PO-{_dateTimeProvider.UtcNow.Year}-{(count + 1):D4}";

        var order = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            BusinessId = targetBusinessId,
            PONumber = poNumber,
            OrderDate = dto.OrderDate,
            ExpectedDeliveryDate = dto.ExpectedDeliveryDate,
            SupplierId = supplier.Id,
            WarehouseId = warehouse.Id,
            Status = PurchaseOrderStatus.Draft,
            Notes = dto.Notes
        };

        decimal subTotal = 0m;
        decimal totalTax = 0m;
        decimal grandTotal = 0m;

        foreach (var itemDto in dto.Items)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == itemDto.ProductId, cancellationToken);
            if (product == null)
            {
                throw new NotFoundException("Product", itemDto.ProductId);
            }

            var gross = itemDto.OrderedQuantity * itemDto.UnitPrice;
            var tax = gross * (itemDto.TaxRate / 100m);
            var lineTotal = gross + tax;

            subTotal += gross;
            totalTax += tax;
            grandTotal += lineTotal;

            order.Items.Add(new PurchaseOrderItem
            {
                Id = Guid.NewGuid(),
                PurchaseOrderId = order.Id,
                ProductId = product.Id,
                OrderedQuantity = itemDto.OrderedQuantity,
                ReceivedQuantity = 0m,
                UnitPrice = itemDto.UnitPrice,
                TaxRate = itemDto.TaxRate,
                TaxAmount = tax,
                TotalAmount = lineTotal,
                Notes = itemDto.Notes
            });
        }

        order.SubTotal = subTotal;
        order.TaxAmount = totalTax;
        order.TotalAmount = grandTotal;

        _context.PurchaseOrders.Add(order);
        await _context.SaveChangesAsync(cancellationToken);

        return await GetPurchaseOrderByIdAsync(order.Id, cancellationToken);
    }

    public async Task<ApiResponse<PurchaseOrderDto>> UpdatePurchaseOrderStatusAsync(Guid id, UpdatePurchaseOrderStatusDto dto, CancellationToken cancellationToken = default)
    {
        var order = await _context.PurchaseOrders.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (order == null)
        {
            throw new NotFoundException("PurchaseOrder", id);
        }

        order.Status = dto.Status;
        await _context.SaveChangesAsync(cancellationToken);

        return await GetPurchaseOrderByIdAsync(id, cancellationToken);
    }

    // ==========================================
    // GOODS RECEIPT NOTES (GRN) & INWARD REPLENISHMENT
    // ==========================================

    public async Task<ApiResponse<PagedResult<GoodsReceiptNoteDto>>> GetGoodsReceiptNotesAsync(
        Guid? purchaseOrderId = null,
        Guid? supplierId = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.GoodsReceiptNotes
            .Include(g => g.Supplier)
            .Include(g => g.Warehouse)
            .Include(g => g.PurchaseOrder)
            .Include(g => g.Items)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p!.UnitOfMeasure)
            .AsNoTracking();

        if (purchaseOrderId.HasValue)
        {
            query = query.Where(g => g.PurchaseOrderId == purchaseOrderId.Value);
        }

        if (supplierId.HasValue)
        {
            query = query.Where(g => g.SupplierId == supplierId.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(g => g.ReceiptDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(g => new GoodsReceiptNoteDto
            {
                Id = g.Id,
                GRNNumber = g.GRNNumber,
                ReceiptDate = g.ReceiptDate,
                PurchaseOrderId = g.PurchaseOrderId,
                PONumber = g.PurchaseOrder != null ? g.PurchaseOrder.PONumber : null,
                SupplierId = g.SupplierId,
                SupplierName = g.Supplier != null ? g.Supplier.Name : string.Empty,
                WarehouseId = g.WarehouseId,
                WarehouseName = g.Warehouse != null ? g.Warehouse.Name : string.Empty,
                SupplierDeliveryNoteNo = g.SupplierDeliveryNoteNo,
                Status = g.Status,
                Notes = g.Notes,
                CreatedOn = g.CreatedOn.UtcDateTime,
                Items = g.Items.Select(i => new GoodsReceiptNoteItemDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    ProductSKU = i.Product != null ? i.Product.SKU : string.Empty,
                    ProductName = i.Product != null ? i.Product.Name : string.Empty,
                    UnitOfMeasure = (i.Product != null && i.Product.UnitOfMeasure != null) ? i.Product.UnitOfMeasure.Code : string.Empty,
                    PurchaseOrderItemId = i.PurchaseOrderItemId,
                    ReceivedQuantity = i.ReceivedQuantity,
                    AcceptedQuantity = i.AcceptedQuantity,
                    RejectedQuantity = i.RejectedQuantity,
                    UnitPrice = i.UnitPrice,
                    RejectionReason = i.RejectionReason,
                    Notes = i.Notes
                }).ToList()
            })
            .ToListAsync(cancellationToken);

        var result = PagedResult<GoodsReceiptNoteDto>.Create(items, totalCount, pageNumber, pageSize);
        return ApiResponse<PagedResult<GoodsReceiptNoteDto>>.SuccessResult(result);
    }

    public async Task<ApiResponse<GoodsReceiptNoteDto>> GetGoodsReceiptNoteByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var g = await _context.GoodsReceiptNotes
            .Include(x => x.Supplier)
            .Include(x => x.Warehouse)
            .Include(x => x.PurchaseOrder)
            .Include(x => x.Items)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p!.UnitOfMeasure)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (g == null)
        {
            throw new NotFoundException("GoodsReceiptNote", id);
        }

        var dto = new GoodsReceiptNoteDto
        {
            Id = g.Id,
            GRNNumber = g.GRNNumber,
            ReceiptDate = g.ReceiptDate,
            PurchaseOrderId = g.PurchaseOrderId,
            PONumber = g.PurchaseOrder?.PONumber,
            SupplierId = g.SupplierId,
            SupplierName = g.Supplier?.Name ?? string.Empty,
            WarehouseId = g.WarehouseId,
            WarehouseName = g.Warehouse?.Name ?? string.Empty,
            SupplierDeliveryNoteNo = g.SupplierDeliveryNoteNo,
            Status = g.Status,
            Notes = g.Notes,
            CreatedOn = g.CreatedOn.UtcDateTime,
            Items = g.Items.Select(i => new GoodsReceiptNoteItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductSKU = i.Product?.SKU ?? string.Empty,
                ProductName = i.Product?.Name ?? string.Empty,
                UnitOfMeasure = i.Product?.UnitOfMeasure?.Code ?? string.Empty,
                PurchaseOrderItemId = i.PurchaseOrderItemId,
                ReceivedQuantity = i.ReceivedQuantity,
                AcceptedQuantity = i.AcceptedQuantity,
                RejectedQuantity = i.RejectedQuantity,
                UnitPrice = i.UnitPrice,
                RejectionReason = i.RejectionReason,
                Notes = i.Notes
            }).ToList()
        };

        return ApiResponse<GoodsReceiptNoteDto>.SuccessResult(dto);
    }

    public async Task<ApiResponse<GoodsReceiptNoteDto>> CreateGoodsReceiptNoteAsync(CreateGoodsReceiptNoteDto dto, CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.BusinessId;
        if (!_currentUserService.IsSuperAdmin && businessId == null)
        {
            throw new BusinessRuleException("No active business tenant context in current session.");
        }

        var targetBusinessId = businessId ?? (await _context.Businesses.Select(b => b.Id).FirstOrDefaultAsync(cancellationToken));

        var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.Id == dto.SupplierId, cancellationToken);
        if (supplier == null)
        {
            throw new NotFoundException("Supplier", dto.SupplierId);
        }

        var warehouse = await _context.Warehouses.FirstOrDefaultAsync(w => w.Id == dto.WarehouseId, cancellationToken);
        if (warehouse == null)
        {
            throw new NotFoundException("Warehouse", dto.WarehouseId);
        }

        PurchaseOrder? po = null;
        if (dto.PurchaseOrderId.HasValue)
        {
            po = await _context.PurchaseOrders
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.Id == dto.PurchaseOrderId.Value, cancellationToken);
        }

        var count = await _context.GoodsReceiptNotes.CountAsync(g => g.BusinessId == targetBusinessId, cancellationToken);
        var grnNumber = $"GRN-{_dateTimeProvider.UtcNow.Year}-{(count + 1):D4}";

        var now = _dateTimeProvider.UtcNow;
        var userEmail = _currentUserService.Email ?? "System";

        var grn = new GoodsReceiptNote
        {
            Id = Guid.NewGuid(),
            BusinessId = targetBusinessId,
            GRNNumber = grnNumber,
            ReceiptDate = dto.ReceiptDate,
            PurchaseOrderId = dto.PurchaseOrderId,
            SupplierId = supplier.Id,
            WarehouseId = warehouse.Id,
            SupplierDeliveryNoteNo = dto.SupplierDeliveryNoteNo,
            Status = GRNStatus.Verified,
            Notes = dto.Notes
        };

        foreach (var itemDto in dto.Items)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == itemDto.ProductId, cancellationToken);
            if (product == null)
            {
                throw new NotFoundException("Product", itemDto.ProductId);
            }

            var accepted = itemDto.AcceptedQuantity > 0 ? itemDto.AcceptedQuantity : (itemDto.ReceivedQuantity - itemDto.RejectedQuantity);
            var unitPrice = itemDto.UnitPrice > 0 ? itemDto.UnitPrice : product.PurchasePrice;

            grn.Items.Add(new GoodsReceiptNoteItem
            {
                Id = Guid.NewGuid(),
                GoodsReceiptNoteId = grn.Id,
                ProductId = product.Id,
                PurchaseOrderItemId = itemDto.PurchaseOrderItemId,
                ReceivedQuantity = itemDto.ReceivedQuantity,
                AcceptedQuantity = accepted,
                RejectedQuantity = itemDto.RejectedQuantity,
                UnitPrice = unitPrice,
                RejectionReason = itemDto.RejectionReason,
                Notes = itemDto.Notes
            });

            // Replenish physical stock & Recalculate Weighted Average Cost
            if (accepted > 0)
            {
                var stock = await _context.InventoryStocks
                    .FirstOrDefaultAsync(s => s.ProductId == product.Id && s.WarehouseId == warehouse.Id, cancellationToken);

                if (stock == null)
                {
                    stock = new InventoryStock
                    {
                        Id = Guid.NewGuid(),
                        BusinessId = targetBusinessId,
                        ProductId = product.Id,
                        WarehouseId = warehouse.Id,
                        QuantityOnHand = accepted,
                        QuantityReserved = 0m,
                        AverageCost = unitPrice,
                        CreatedOn = now,
                        CreatedBy = userEmail
                    };
                    _context.InventoryStocks.Add(stock);
                }
                else
                {
                    var currentTotalVal = stock.QuantityOnHand * stock.AverageCost;
                    var inwardVal = accepted * unitPrice;
                    var newTotalQty = stock.QuantityOnHand + accepted;

                    stock.AverageCost = newTotalQty > 0 ? (currentTotalVal + inwardVal) / newTotalQty : unitPrice;
                    stock.QuantityOnHand += accepted;
                    stock.UpdatedOn = now;
                    stock.UpdatedBy = userEmail;
                }

                // Immutable transaction ledger
                _context.StockTransactions.Add(new StockTransaction
                {
                    Id = Guid.NewGuid(),
                    BusinessId = targetBusinessId,
                    ProductId = product.Id,
                    WarehouseId = warehouse.Id,
                    TransactionType = StockTransactionType.Inward,
                    Quantity = accepted,
                    UnitPrice = unitPrice,
                    TotalAmount = accepted * unitPrice,
                    ReferenceType = "GoodsReceiptNote",
                    ReferenceId = grn.GRNNumber,
                    Notes = $"Received from {supplier.Name}. Challan: {dto.SupplierDeliveryNoteNo}",
                    Timestamp = now,
                    CreatedOn = now,
                    CreatedBy = userEmail
                });
            }

            // Update PO line received quantity if linked
            if (po != null && itemDto.PurchaseOrderItemId.HasValue)
            {
                var poItem = po.Items.FirstOrDefault(i => i.Id == itemDto.PurchaseOrderItemId.Value);
                if (poItem != null)
                {
                    poItem.ReceivedQuantity += accepted;
                }
            }
        }

        // Update overall PO status
        if (po != null)
        {
            var isFullyReceived = po.Items.All(i => i.ReceivedQuantity >= i.OrderedQuantity);
            po.Status = isFullyReceived ? PurchaseOrderStatus.Received : PurchaseOrderStatus.PartiallyReceived;
        }

        _context.GoodsReceiptNotes.Add(grn);
        await _context.SaveChangesAsync(cancellationToken);

        return await GetGoodsReceiptNoteByIdAsync(grn.Id, cancellationToken);
    }

    // ==========================================
    // PURCHASE BILLS (ACCOUNTS PAYABLE)
    // ==========================================

    public async Task<ApiResponse<PagedResult<PurchaseBillDto>>> GetPurchaseBillsAsync(
        Guid? supplierId = null,
        BillStatus? status = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.PurchaseBills
            .Include(b => b.Supplier)
            .Include(b => b.Warehouse)
            .Include(b => b.PurchaseOrder)
            .Include(b => b.Items)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p!.UnitOfMeasure)
            .Include(b => b.Payments)
            .AsNoTracking();

        if (supplierId.HasValue)
        {
            query = query.Where(b => b.SupplierId == supplierId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(b => b.Status == status.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(b => b.BillDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new PurchaseBillDto
            {
                Id = b.Id,
                BillNumber = b.BillNumber,
                VendorInvoiceNumber = b.VendorInvoiceNumber,
                BillDate = b.BillDate,
                DueDate = b.DueDate,
                PurchaseOrderId = b.PurchaseOrderId,
                PONumber = b.PurchaseOrder != null ? b.PurchaseOrder.PONumber : null,
                SupplierId = b.SupplierId,
                SupplierName = b.Supplier != null ? b.Supplier.Name : string.Empty,
                SupplierCode = b.Supplier != null ? b.Supplier.SupplierCode : string.Empty,
                SupplierGSTIN = b.Supplier != null ? b.Supplier.GSTIN : null,
                WarehouseId = b.WarehouseId,
                WarehouseName = b.Warehouse != null ? b.Warehouse.Name : string.Empty,
                Status = b.Status,
                SubTotal = b.SubTotal,
                TaxAmount = b.TaxAmount,
                CgstAmount = b.CgstAmount,
                SgstAmount = b.SgstAmount,
                IgstAmount = b.IgstAmount,
                TotalAmount = b.TotalAmount,
                PaidAmount = b.PaidAmount,
                BalanceAmount = b.BalanceAmount,
                Notes = b.Notes,
                CreatedOn = b.CreatedOn.UtcDateTime,
                Items = b.Items.Select(i => new PurchaseBillItemDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    ProductSKU = i.Product != null ? i.Product.SKU : string.Empty,
                    ProductName = i.Product != null ? i.Product.Name : string.Empty,
                    UnitOfMeasure = (i.Product != null && i.Product.UnitOfMeasure != null) ? i.Product.UnitOfMeasure.Code : string.Empty,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    TaxRate = i.TaxRate,
                    TaxAmount = i.TaxAmount,
                    TotalAmount = i.TotalAmount,
                    Notes = i.Notes
                }).ToList(),
                Payments = b.Payments.Select(p => new VendorPaymentDto
                {
                    Id = p.Id,
                    PaymentNumber = p.PaymentNumber,
                    PurchaseBillId = p.PurchaseBillId,
                    BillNumber = b.BillNumber,
                    SupplierId = p.SupplierId,
                    SupplierName = b.Supplier != null ? b.Supplier.Name : string.Empty,
                    PaymentDate = p.PaymentDate,
                    Amount = p.Amount,
                    Method = p.Method,
                    ReferenceNumber = p.ReferenceNumber,
                    Notes = p.Notes,
                    CreatedOn = p.CreatedOn.UtcDateTime
                }).ToList()
            })
            .ToListAsync(cancellationToken);

        var result = PagedResult<PurchaseBillDto>.Create(items, totalCount, pageNumber, pageSize);
        return ApiResponse<PagedResult<PurchaseBillDto>>.SuccessResult(result);
    }

    public async Task<ApiResponse<PurchaseBillDto>> GetPurchaseBillByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var b = await _context.PurchaseBills
            .Include(x => x.Supplier)
            .Include(x => x.Warehouse)
            .Include(x => x.PurchaseOrder)
            .Include(x => x.Items)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p!.UnitOfMeasure)
            .Include(x => x.Payments)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (b == null)
        {
            throw new NotFoundException("PurchaseBill", id);
        }

        var dto = new PurchaseBillDto
        {
            Id = b.Id,
            BillNumber = b.BillNumber,
            VendorInvoiceNumber = b.VendorInvoiceNumber,
            BillDate = b.BillDate,
            DueDate = b.DueDate,
            PurchaseOrderId = b.PurchaseOrderId,
            PONumber = b.PurchaseOrder?.PONumber,
            SupplierId = b.SupplierId,
            SupplierName = b.Supplier?.Name ?? string.Empty,
            SupplierCode = b.Supplier?.SupplierCode ?? string.Empty,
            SupplierGSTIN = b.Supplier?.GSTIN,
            WarehouseId = b.WarehouseId,
            WarehouseName = b.Warehouse?.Name ?? string.Empty,
            Status = b.Status,
            SubTotal = b.SubTotal,
            TaxAmount = b.TaxAmount,
            CgstAmount = b.CgstAmount,
            SgstAmount = b.SgstAmount,
            IgstAmount = b.IgstAmount,
            TotalAmount = b.TotalAmount,
            PaidAmount = b.PaidAmount,
            BalanceAmount = b.BalanceAmount,
            Notes = b.Notes,
            CreatedOn = b.CreatedOn.UtcDateTime,
            Items = b.Items.Select(i => new PurchaseBillItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductSKU = i.Product?.SKU ?? string.Empty,
                ProductName = i.Product?.Name ?? string.Empty,
                UnitOfMeasure = i.Product?.UnitOfMeasure?.Code ?? string.Empty,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                TaxRate = i.TaxRate,
                TaxAmount = i.TaxAmount,
                TotalAmount = i.TotalAmount,
                Notes = i.Notes
            }).ToList(),
            Payments = b.Payments.Select(p => new VendorPaymentDto
            {
                Id = p.Id,
                PaymentNumber = p.PaymentNumber,
                PurchaseBillId = p.PurchaseBillId,
                BillNumber = b.BillNumber,
                SupplierId = p.SupplierId,
                SupplierName = b.Supplier?.Name ?? string.Empty,
                PaymentDate = p.PaymentDate,
                Amount = p.Amount,
                Method = p.Method,
                ReferenceNumber = p.ReferenceNumber,
                Notes = p.Notes,
                CreatedOn = p.CreatedOn.UtcDateTime
            }).ToList()
        };

        return ApiResponse<PurchaseBillDto>.SuccessResult(dto);
    }

    public async Task<ApiResponse<PurchaseBillDto>> CreatePurchaseBillAsync(CreatePurchaseBillDto dto, CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.BusinessId;
        if (!_currentUserService.IsSuperAdmin && businessId == null)
        {
            throw new BusinessRuleException("No active business tenant context in current session.");
        }

        var targetBusinessId = businessId ?? (await _context.Businesses.Select(b => b.Id).FirstOrDefaultAsync(cancellationToken));

        var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.Id == dto.SupplierId, cancellationToken);
        if (supplier == null)
        {
            throw new NotFoundException("Supplier", dto.SupplierId);
        }

        var warehouse = await _context.Warehouses.FirstOrDefaultAsync(w => w.Id == dto.WarehouseId, cancellationToken);
        if (warehouse == null)
        {
            throw new NotFoundException("Warehouse", dto.WarehouseId);
        }

        var count = await _context.PurchaseBills.CountAsync(b => b.BusinessId == targetBusinessId, cancellationToken);
        var billNumber = $"BILL-{_dateTimeProvider.UtcNow.Year}-{(count + 1):D4}";

        var bill = new PurchaseBill
        {
            Id = Guid.NewGuid(),
            BusinessId = targetBusinessId,
            BillNumber = billNumber,
            VendorInvoiceNumber = dto.VendorInvoiceNumber?.Trim(),
            BillDate = dto.BillDate,
            DueDate = dto.DueDate,
            PurchaseOrderId = dto.PurchaseOrderId,
            SupplierId = supplier.Id,
            WarehouseId = warehouse.Id,
            Status = BillStatus.Posted,
            Notes = dto.Notes
        };

        decimal subTotal = 0m;
        decimal totalTax = 0m;
        decimal grandTotal = 0m;
        decimal cgstTotal = 0m;
        decimal sgstTotal = 0m;
        decimal igstTotal = 0m;

        foreach (var itemDto in dto.Items)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == itemDto.ProductId, cancellationToken);
            if (product == null)
            {
                throw new NotFoundException("Product", itemDto.ProductId);
            }

            var gross = itemDto.Quantity * itemDto.UnitPrice;
            var tax = gross * (itemDto.TaxRate / 100m);
            var lineTotal = gross + tax;

            subTotal += gross;
            totalTax += tax;
            grandTotal += lineTotal;

            if (dto.IsInterstate)
            {
                igstTotal += tax;
            }
            else
            {
                cgstTotal += tax / 2m;
                sgstTotal += tax / 2m;
            }

            bill.Items.Add(new PurchaseBillItem
            {
                Id = Guid.NewGuid(),
                PurchaseBillId = bill.Id,
                ProductId = product.Id,
                Quantity = itemDto.Quantity,
                UnitPrice = itemDto.UnitPrice,
                TaxRate = itemDto.TaxRate,
                TaxAmount = tax,
                TotalAmount = lineTotal,
                Notes = itemDto.Notes
            });
        }

        bill.SubTotal = subTotal;
        bill.TaxAmount = totalTax;
        bill.CgstAmount = cgstTotal;
        bill.SgstAmount = sgstTotal;
        bill.IgstAmount = igstTotal;
        bill.TotalAmount = grandTotal;
        bill.PaidAmount = 0m;
        bill.BalanceAmount = grandTotal;

        // Increase supplier accounts payable balance
        supplier.OutstandingPayable += grandTotal;

        _context.PurchaseBills.Add(bill);
        await _context.SaveChangesAsync(cancellationToken);

        return await GetPurchaseBillByIdAsync(bill.Id, cancellationToken);
    }

    public async Task<ApiResponse<bool>> CancelPurchaseBillAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var bill = await _context.PurchaseBills
            .Include(b => b.Supplier)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

        if (bill == null)
        {
            throw new NotFoundException("PurchaseBill", id);
        }

        if (bill.PaidAmount > 0)
        {
            throw new BusinessRuleException("Cannot cancel vendor bill with payments recorded. Reverse payments first.");
        }

        if (bill.Status == BillStatus.Cancelled)
        {
            throw new BusinessRuleException("Bill is already cancelled.");
        }

        if (bill.Supplier != null)
        {
            bill.Supplier.OutstandingPayable = Math.Max(0m, bill.Supplier.OutstandingPayable - bill.BalanceAmount);
        }

        bill.Status = BillStatus.Cancelled;
        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.SuccessResult(true, "Purchase bill cancelled successfully.");
    }

    // ==========================================
    // VENDOR DISBURSEMENTS (PAYMENTS)
    // ==========================================

    public async Task<ApiResponse<VendorPaymentDto>> RecordVendorPaymentAsync(RecordVendorPaymentDto dto, CancellationToken cancellationToken = default)
    {
        var bill = await _context.PurchaseBills
            .Include(b => b.Supplier)
            .FirstOrDefaultAsync(b => b.Id == dto.PurchaseBillId, cancellationToken);

        if (bill == null)
        {
            throw new NotFoundException("PurchaseBill", dto.PurchaseBillId);
        }

        if (bill.Status == BillStatus.Paid)
        {
            throw new BusinessRuleException("Vendor bill is already fully settled.");
        }

        if (bill.Status == BillStatus.Cancelled)
        {
            throw new BusinessRuleException("Cannot pay against a cancelled bill.");
        }

        if (dto.Amount > bill.BalanceAmount)
        {
            throw new BusinessRuleException($"Payment amount ₹{dto.Amount:N2} exceeds bill balance of ₹{bill.BalanceAmount:N2}.");
        }

        var count = await _context.VendorPayments.CountAsync(p => p.BusinessId == bill.BusinessId, cancellationToken);
        var paymentNumber = $"VPAY-{_dateTimeProvider.UtcNow.Year}-{(count + 1):D4}";

        var payment = new VendorPayment
        {
            Id = Guid.NewGuid(),
            BusinessId = bill.BusinessId,
            PaymentNumber = paymentNumber,
            PurchaseBillId = bill.Id,
            SupplierId = bill.SupplierId,
            PaymentDate = dto.PaymentDate,
            Amount = dto.Amount,
            Method = dto.Method,
            ReferenceNumber = dto.ReferenceNumber,
            Notes = dto.Notes
        };

        bill.PaidAmount += dto.Amount;
        bill.BalanceAmount -= dto.Amount;
        bill.Status = bill.BalanceAmount == 0m ? BillStatus.Paid : BillStatus.PartiallyPaid;

        if (bill.Supplier != null)
        {
            bill.Supplier.OutstandingPayable = Math.Max(0m, bill.Supplier.OutstandingPayable - dto.Amount);
        }

        _context.VendorPayments.Add(payment);
        await _context.SaveChangesAsync(cancellationToken);

        var resultDto = new VendorPaymentDto
        {
            Id = payment.Id,
            PaymentNumber = payment.PaymentNumber,
            PurchaseBillId = payment.PurchaseBillId,
            BillNumber = bill.BillNumber,
            SupplierId = payment.SupplierId,
            SupplierName = bill.Supplier?.Name ?? string.Empty,
            PaymentDate = payment.PaymentDate,
            Amount = payment.Amount,
            Method = payment.Method,
            ReferenceNumber = payment.ReferenceNumber,
            Notes = payment.Notes,
            CreatedOn = payment.CreatedOn.UtcDateTime
        };

        return ApiResponse<VendorPaymentDto>.SuccessResult(resultDto, "Vendor payment recorded successfully.");
    }

    // ==========================================
    // PROCUREMENT METRICS
    // ==========================================

    public async Task<ApiResponse<PurchaseSummaryDto>> GetPurchaseSummaryAsync(CancellationToken cancellationToken = default)
    {
        var now = _dateTimeProvider.UtcNow;

        var activeBills = await _context.PurchaseBills
            .AsNoTracking()
            .Where(b => b.Status != BillStatus.Cancelled)
            .ToListAsync(cancellationToken);

        var totalPurchases = activeBills.Sum(b => b.TotalAmount);
        var totalDisbursed = activeBills.Sum(b => b.PaidAmount);
        var totalOutstanding = activeBills.Sum(b => b.BalanceAmount);
        var totalOverdue = activeBills
            .Where(b => b.BalanceAmount > 0 && b.DueDate < now)
            .Sum(b => b.BalanceAmount);

        var pendingOrdersCount = await _context.PurchaseOrders
            .AsNoTracking()
            .CountAsync(p => p.Status == PurchaseOrderStatus.Draft || p.Status == PurchaseOrderStatus.Approved || p.Status == PurchaseOrderStatus.PartiallyReceived, cancellationToken);

        var activeSuppliersCount = await _context.Suppliers
            .AsNoTracking()
            .CountAsync(s => s.IsActive, cancellationToken);

        var summary = new PurchaseSummaryDto
        {
            TotalPurchases = totalPurchases,
            TotalDisbursed = totalDisbursed,
            TotalOutstandingPayables = totalOutstanding,
            TotalOverduePayables = totalOverdue,
            TotalBillsCount = activeBills.Count,
            PendingOrdersCount = pendingOrdersCount,
            ActiveSuppliersCount = activeSuppliersCount
        };

        return ApiResponse<PurchaseSummaryDto>.SuccessResult(summary);
    }
}
