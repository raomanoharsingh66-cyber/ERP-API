using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Sales;
using BizFlow.Domain.Entities;
using BizFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BizFlow.Infrastructure.Services;

public class SalesService : ISalesService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public SalesService(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    // ==========================================
    // SALES ORDERS
    // ==========================================

    public async Task<ApiResponse<PagedResult<SalesOrderDto>>> GetSalesOrdersAsync(
        Guid? customerId = null,
        OrderStatus? status = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.SalesOrders
            .Include(o => o.Customer)
            .Include(o => o.Warehouse)
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p!.UnitOfMeasure)
            .AsNoTracking();

        if (customerId.HasValue)
        {
            query = query.Where(o => o.CustomerId == customerId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(o => o.OrderDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new SalesOrderDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                OrderDate = o.OrderDate,
                ExpectedDeliveryDate = o.ExpectedDeliveryDate,
                CustomerId = o.CustomerId,
                CustomerName = o.Customer != null ? o.Customer.Name : string.Empty,
                CustomerCode = o.Customer != null ? o.Customer.CustomerCode : string.Empty,
                WarehouseId = o.WarehouseId,
                WarehouseName = o.Warehouse != null ? o.Warehouse.Name : string.Empty,
                Status = o.Status,
                SubTotal = o.SubTotal,
                DiscountAmount = o.DiscountAmount,
                TaxAmount = o.TaxAmount,
                TotalAmount = o.TotalAmount,
                Notes = o.Notes,
                CreatedOn = o.CreatedOn,
                Items = o.Items.Select(i => new SalesOrderItemDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    ProductSKU = i.Product != null ? i.Product.SKU : string.Empty,
                    ProductName = i.Product != null ? i.Product.Name : string.Empty,
                    UnitOfMeasure = (i.Product != null && i.Product.UnitOfMeasure != null) ? i.Product.UnitOfMeasure.Code : string.Empty,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    DiscountPercentage = i.DiscountPercentage,
                    DiscountAmount = i.DiscountAmount,
                    TaxRate = i.TaxRate,
                    TaxAmount = i.TaxAmount,
                    TotalAmount = i.TotalAmount,
                    Notes = i.Notes
                }).ToList()
            })
            .ToListAsync(cancellationToken);

        var result = PagedResult<SalesOrderDto>.Create(items, totalCount, pageNumber, pageSize);
        return ApiResponse<PagedResult<SalesOrderDto>>.SuccessResult(result);
    }

    public async Task<ApiResponse<SalesOrderDto>> GetSalesOrderByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var o = await _context.SalesOrders
            .Include(x => x.Customer)
            .Include(x => x.Warehouse)
            .Include(x => x.Items)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p!.UnitOfMeasure)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (o == null)
        {
            throw new NotFoundException("SalesOrder", id);
        }

        var dto = new SalesOrderDto
        {
            Id = o.Id,
            OrderNumber = o.OrderNumber,
            OrderDate = o.OrderDate,
            ExpectedDeliveryDate = o.ExpectedDeliveryDate,
            CustomerId = o.CustomerId,
            CustomerName = o.Customer?.Name ?? string.Empty,
            CustomerCode = o.Customer?.CustomerCode ?? string.Empty,
            WarehouseId = o.WarehouseId,
            WarehouseName = o.Warehouse?.Name ?? string.Empty,
            Status = o.Status,
            SubTotal = o.SubTotal,
            DiscountAmount = o.DiscountAmount,
            TaxAmount = o.TaxAmount,
            TotalAmount = o.TotalAmount,
            Notes = o.Notes,
            CreatedOn = o.CreatedOn.UtcDateTime,
            Items = o.Items.Select(i => new SalesOrderItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductSKU = i.Product?.SKU ?? string.Empty,
                ProductName = i.Product?.Name ?? string.Empty,
                UnitOfMeasure = i.Product?.UnitOfMeasure?.Code ?? string.Empty,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                DiscountPercentage = i.DiscountPercentage,
                DiscountAmount = i.DiscountAmount,
                TaxRate = i.TaxRate,
                TaxAmount = i.TaxAmount,
                TotalAmount = i.TotalAmount,
                Notes = i.Notes
            }).ToList()
        };

        return ApiResponse<SalesOrderDto>.SuccessResult(dto);
    }

    public async Task<ApiResponse<SalesOrderDto>> CreateSalesOrderAsync(CreateSalesOrderDto dto, CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.BusinessId;
        if (!_currentUserService.IsSuperAdmin && businessId == null)
        {
            throw new BusinessRuleException("No active business tenant context in current session.");
        }

        var targetBusinessId = businessId ?? (await _context.Businesses.Select(b => b.Id).FirstOrDefaultAsync(cancellationToken));

        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == dto.CustomerId, cancellationToken);
        if (customer == null)
        {
            throw new NotFoundException("Customer", dto.CustomerId);
        }

        var warehouse = await _context.Warehouses.FirstOrDefaultAsync(w => w.Id == dto.WarehouseId, cancellationToken);
        if (warehouse == null)
        {
            throw new NotFoundException("Warehouse", dto.WarehouseId);
        }

        var count = await _context.SalesOrders.CountAsync(o => o.BusinessId == targetBusinessId, cancellationToken);
        var orderNumber = $"SO-{_dateTimeProvider.UtcNow.Year}-{(count + 1):D4}";

        var order = new SalesOrder
        {
            Id = Guid.NewGuid(),
            BusinessId = targetBusinessId,
            OrderNumber = orderNumber,
            OrderDate = dto.OrderDate,
            ExpectedDeliveryDate = dto.ExpectedDeliveryDate,
            CustomerId = customer.Id,
            WarehouseId = warehouse.Id,
            Status = OrderStatus.Draft,
            Notes = dto.Notes
        };

        decimal subTotal = 0m;
        decimal totalDiscount = 0m;
        decimal totalTax = 0m;
        decimal grandTotal = 0m;

        foreach (var itemDto in dto.Items)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == itemDto.ProductId, cancellationToken);
            if (product == null)
            {
                throw new NotFoundException("Product", itemDto.ProductId);
            }

            var gross = itemDto.Quantity * itemDto.UnitPrice;
            var disc = gross * (itemDto.DiscountPercentage / 100m);
            var net = gross - disc;
            var tax = net * (itemDto.TaxRate / 100m);
            var lineTotal = net + tax;

            subTotal += gross;
            totalDiscount += disc;
            totalTax += tax;
            grandTotal += lineTotal;

            order.Items.Add(new SalesOrderItem
            {
                Id = Guid.NewGuid(),
                SalesOrderId = order.Id,
                ProductId = product.Id,
                Quantity = itemDto.Quantity,
                UnitPrice = itemDto.UnitPrice,
                DiscountPercentage = itemDto.DiscountPercentage,
                DiscountAmount = disc,
                TaxRate = itemDto.TaxRate,
                TaxAmount = tax,
                TotalAmount = lineTotal,
                Notes = itemDto.Notes
            });
        }

        order.SubTotal = subTotal;
        order.DiscountAmount = totalDiscount;
        order.TaxAmount = totalTax;
        order.TotalAmount = grandTotal;

        _context.SalesOrders.Add(order);
        await _context.SaveChangesAsync(cancellationToken);

        return await GetSalesOrderByIdAsync(order.Id, cancellationToken);
    }

    public async Task<ApiResponse<SalesOrderDto>> UpdateOrderStatusAsync(Guid id, UpdateOrderStatusDto dto, CancellationToken cancellationToken = default)
    {
        var order = await _context.SalesOrders.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (order == null)
        {
            throw new NotFoundException("SalesOrder", id);
        }

        order.Status = dto.Status;
        await _context.SaveChangesAsync(cancellationToken);

        return await GetSalesOrderByIdAsync(id, cancellationToken);
    }

    // ==========================================
    // SALES INVOICES & INVENTORY DEPLETION
    // ==========================================

    public async Task<ApiResponse<PagedResult<SalesInvoiceDto>>> GetSalesInvoicesAsync(
        Guid? customerId = null,
        InvoiceStatus? status = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.SalesInvoices
            .Include(i => i.Customer)
            .Include(i => i.Warehouse)
            .Include(i => i.SalesOrder)
            .Include(i => i.Items)
                .ThenInclude(it => it.Product)
                    .ThenInclude(p => p!.UnitOfMeasure)
            .Include(i => i.Payments)
            .AsNoTracking();

        if (customerId.HasValue)
        {
            query = query.Where(i => i.CustomerId == customerId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(i => i.Status == status.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(i => i.InvoiceDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(i => new SalesInvoiceDto
            {
                Id = i.Id,
                InvoiceNumber = i.InvoiceNumber,
                InvoiceDate = i.InvoiceDate,
                DueDate = i.DueDate,
                SalesOrderId = i.SalesOrderId,
                OrderNumber = i.SalesOrder != null ? i.SalesOrder.OrderNumber : null,
                CustomerId = i.CustomerId,
                CustomerName = i.Customer != null ? i.Customer.Name : string.Empty,
                CustomerCode = i.Customer != null ? i.Customer.CustomerCode : string.Empty,
                CustomerGSTIN = i.Customer != null ? i.Customer.GSTIN : null,
                CustomerBillingAddress = i.Customer != null ? i.Customer.BillingAddress : null,
                CustomerBillingCity = i.Customer != null ? i.Customer.BillingCity : null,
                CustomerBillingState = i.Customer != null ? i.Customer.BillingState : null,
                WarehouseId = i.WarehouseId,
                WarehouseName = i.Warehouse != null ? i.Warehouse.Name : string.Empty,
                Status = i.Status,
                SubTotal = i.SubTotal,
                DiscountAmount = i.DiscountAmount,
                TaxAmount = i.TaxAmount,
                CgstAmount = i.CgstAmount,
                SgstAmount = i.SgstAmount,
                IgstAmount = i.IgstAmount,
                TotalAmount = i.TotalAmount,
                PaidAmount = i.PaidAmount,
                BalanceAmount = i.BalanceAmount,
                Notes = i.Notes,
                CreatedOn = i.CreatedOn.UtcDateTime,
                Items = i.Items.Select(it => new SalesInvoiceItemDto
                {
                    Id = it.Id,
                    ProductId = it.ProductId,
                    ProductSKU = it.Product != null ? it.Product.SKU : string.Empty,
                    ProductName = it.Product != null ? it.Product.Name : string.Empty,
                    UnitOfMeasure = (it.Product != null && it.Product.UnitOfMeasure != null) ? it.Product.UnitOfMeasure.Code : string.Empty,
                    Quantity = it.Quantity,
                    UnitPrice = it.UnitPrice,
                    DiscountPercentage = it.DiscountPercentage,
                    DiscountAmount = it.DiscountAmount,
                    TaxRate = it.TaxRate,
                    TaxAmount = it.TaxAmount,
                    TotalAmount = it.TotalAmount,
                    Notes = it.Notes
                }).ToList(),
                Payments = i.Payments.Select(p => new SalesPaymentDto
                {
                    Id = p.Id,
                    PaymentNumber = p.PaymentNumber,
                    SalesInvoiceId = p.SalesInvoiceId,
                    InvoiceNumber = i.InvoiceNumber,
                    CustomerId = p.CustomerId,
                    CustomerName = i.Customer != null ? i.Customer.Name : string.Empty,
                    PaymentDate = p.PaymentDate,
                    Amount = p.Amount,
                    Method = p.Method,
                    ReferenceNumber = p.ReferenceNumber,
                    Notes = p.Notes,
                    CreatedOn = p.CreatedOn.UtcDateTime
                }).ToList()
            })
            .ToListAsync(cancellationToken);

        var result = PagedResult<SalesInvoiceDto>.Create(items, totalCount, pageNumber, pageSize);
        return ApiResponse<PagedResult<SalesInvoiceDto>>.SuccessResult(result);
    }

    public async Task<ApiResponse<SalesInvoiceDto>> GetSalesInvoiceByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var i = await _context.SalesInvoices
            .Include(x => x.Customer)
            .Include(x => x.Warehouse)
            .Include(x => x.SalesOrder)
            .Include(x => x.Items)
                .ThenInclude(it => it.Product)
                    .ThenInclude(p => p!.UnitOfMeasure)
            .Include(x => x.Payments)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (i == null)
        {
            throw new NotFoundException("SalesInvoice", id);
        }

        var dto = new SalesInvoiceDto
        {
            Id = i.Id,
            InvoiceNumber = i.InvoiceNumber,
            InvoiceDate = i.InvoiceDate,
            DueDate = i.DueDate,
            SalesOrderId = i.SalesOrderId,
            OrderNumber = i.SalesOrder?.OrderNumber,
            CustomerId = i.CustomerId,
            CustomerName = i.Customer?.Name ?? string.Empty,
            CustomerCode = i.Customer?.CustomerCode ?? string.Empty,
            CustomerGSTIN = i.Customer?.GSTIN,
            CustomerBillingAddress = i.Customer?.BillingAddress,
            CustomerBillingCity = i.Customer?.BillingCity,
            CustomerBillingState = i.Customer?.BillingState,
            WarehouseId = i.WarehouseId,
            WarehouseName = i.Warehouse?.Name ?? string.Empty,
            Status = i.Status,
            SubTotal = i.SubTotal,
            DiscountAmount = i.DiscountAmount,
            TaxAmount = i.TaxAmount,
            CgstAmount = i.CgstAmount,
            SgstAmount = i.SgstAmount,
            IgstAmount = i.IgstAmount,
            TotalAmount = i.TotalAmount,
            PaidAmount = i.PaidAmount,
            BalanceAmount = i.BalanceAmount,
            Notes = i.Notes,
            CreatedOn = i.CreatedOn.UtcDateTime,
            Items = i.Items.Select(it => new SalesInvoiceItemDto
            {
                Id = it.Id,
                ProductId = it.ProductId,
                ProductSKU = it.Product?.SKU ?? string.Empty,
                ProductName = it.Product?.Name ?? string.Empty,
                UnitOfMeasure = it.Product?.UnitOfMeasure?.Code ?? string.Empty,
                Quantity = it.Quantity,
                UnitPrice = it.UnitPrice,
                DiscountPercentage = it.DiscountPercentage,
                DiscountAmount = it.DiscountAmount,
                TaxRate = it.TaxRate,
                TaxAmount = it.TaxAmount,
                TotalAmount = it.TotalAmount,
                Notes = it.Notes
            }).ToList(),
            Payments = i.Payments.Select(p => new SalesPaymentDto
            {
                Id = p.Id,
                PaymentNumber = p.PaymentNumber,
                SalesInvoiceId = p.SalesInvoiceId,
                InvoiceNumber = i.InvoiceNumber,
                CustomerId = p.CustomerId,
                CustomerName = i.Customer?.Name ?? string.Empty,
                PaymentDate = p.PaymentDate,
                Amount = p.Amount,
                Method = p.Method,
                ReferenceNumber = p.ReferenceNumber,
                Notes = p.Notes,
                CreatedOn = p.CreatedOn.UtcDateTime
            }).ToList()
        };

        return ApiResponse<SalesInvoiceDto>.SuccessResult(dto);
    }

    public async Task<ApiResponse<SalesInvoiceDto>> CreateSalesInvoiceAsync(CreateSalesInvoiceDto dto, CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.BusinessId;
        if (!_currentUserService.IsSuperAdmin && businessId == null)
        {
            throw new BusinessRuleException("No active business tenant context in current session.");
        }

        var targetBusinessId = businessId ?? (await _context.Businesses.Select(b => b.Id).FirstOrDefaultAsync(cancellationToken));

        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == dto.CustomerId, cancellationToken);
        if (customer == null)
        {
            throw new NotFoundException("Customer", dto.CustomerId);
        }

        var warehouse = await _context.Warehouses.FirstOrDefaultAsync(w => w.Id == dto.WarehouseId, cancellationToken);
        if (warehouse == null)
        {
            throw new NotFoundException("Warehouse", dto.WarehouseId);
        }

        var count = await _context.SalesInvoices.CountAsync(i => i.BusinessId == targetBusinessId, cancellationToken);
        var invoiceNumber = $"INV-{_dateTimeProvider.UtcNow.Year}-{(count + 1):D4}";

        var invoice = new SalesInvoice
        {
            Id = Guid.NewGuid(),
            BusinessId = targetBusinessId,
            InvoiceNumber = invoiceNumber,
            InvoiceDate = dto.InvoiceDate,
            DueDate = dto.DueDate,
            SalesOrderId = dto.SalesOrderId,
            CustomerId = customer.Id,
            WarehouseId = warehouse.Id,
            Status = InvoiceStatus.Issued,
            Notes = dto.Notes
        };

        decimal subTotal = 0m;
        decimal totalDiscount = 0m;
        decimal totalTax = 0m;
        decimal grandTotal = 0m;
        decimal cgstTotal = 0m;
        decimal sgstTotal = 0m;
        decimal igstTotal = 0m;

        var now = _dateTimeProvider.UtcNow;
        var userEmail = _currentUserService.Email ?? "System";

        // Validate stock availability and calculate items
        foreach (var itemDto in dto.Items)
        {
            var product = await _context.Products
                .Include(p => p.UnitOfMeasure)
                .FirstOrDefaultAsync(p => p.Id == itemDto.ProductId, cancellationToken);

            if (product == null)
            {
                throw new NotFoundException("Product", itemDto.ProductId);
            }

            // Verify stock in the warehouse
            var stock = await _context.InventoryStocks
                .FirstOrDefaultAsync(s => s.ProductId == itemDto.ProductId && s.WarehouseId == dto.WarehouseId, cancellationToken);

            if (stock == null || stock.QuantityAvailable < itemDto.Quantity)
            {
                var available = stock?.QuantityAvailable ?? 0m;
                var unitCode = product.UnitOfMeasure?.Code ?? "units";
                throw new BusinessRuleException(
                    $"Insufficient available stock for '{product.Name}' ({product.SKU}) in warehouse '{warehouse.Name}'. Available: {available} {unitCode}, Requested: {itemDto.Quantity} {unitCode}.");
            }

            var gross = itemDto.Quantity * itemDto.UnitPrice;
            var disc = gross * (itemDto.DiscountPercentage / 100m);
            var net = gross - disc;
            var tax = net * (itemDto.TaxRate / 100m);
            var lineTotal = net + tax;

            subTotal += gross;
            totalDiscount += disc;
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

            invoice.Items.Add(new SalesInvoiceItem
            {
                Id = Guid.NewGuid(),
                SalesInvoiceId = invoice.Id,
                ProductId = product.Id,
                Quantity = itemDto.Quantity,
                UnitPrice = itemDto.UnitPrice,
                DiscountPercentage = itemDto.DiscountPercentage,
                DiscountAmount = disc,
                TaxRate = itemDto.TaxRate,
                TaxAmount = tax,
                TotalAmount = lineTotal,
                Notes = itemDto.Notes
            });

            // Automatically deduct stock and register outward transaction
            stock.QuantityOnHand -= itemDto.Quantity;
            stock.UpdatedOn = now;
            stock.UpdatedBy = userEmail;

            _context.StockTransactions.Add(new StockTransaction
            {
                Id = Guid.NewGuid(),
                BusinessId = targetBusinessId,
                ProductId = product.Id,
                WarehouseId = warehouse.Id,
                TransactionType = StockTransactionType.Outward,
                Quantity = itemDto.Quantity,
                UnitPrice = stock.AverageCost,
                TotalAmount = itemDto.Quantity * stock.AverageCost,
                ReferenceType = "SalesInvoice",
                ReferenceId = invoice.InvoiceNumber,
                Notes = $"Billed to {customer.Name} ({customer.CustomerCode})",
                Timestamp = now,
                CreatedOn = now,
                CreatedBy = userEmail
            });
        }

        // Credit limit validation check
        if (customer.CreditLimit > 0 && (customer.OutstandingBalance + grandTotal) > customer.CreditLimit)
        {
            throw new BusinessRuleException(
                $"Cannot issue invoice: Exceeds customer credit limit. Credit Limit: ₹{customer.CreditLimit:N2}, Current Outstanding: ₹{customer.OutstandingBalance:N2}, New Invoice: ₹{grandTotal:N2}.");
        }

        invoice.SubTotal = subTotal;
        invoice.DiscountAmount = totalDiscount;
        invoice.TaxAmount = totalTax;
        invoice.CgstAmount = cgstTotal;
        invoice.SgstAmount = sgstTotal;
        invoice.IgstAmount = igstTotal;
        invoice.TotalAmount = grandTotal;
        invoice.PaidAmount = 0m;
        invoice.BalanceAmount = grandTotal;

        // Increase customer outstanding receivables
        customer.OutstandingBalance += grandTotal;

        // If created from a Sales Order, mark the order as Confirmed / Shipped
        if (dto.SalesOrderId.HasValue)
        {
            var linkedOrder = await _context.SalesOrders.FirstOrDefaultAsync(o => o.Id == dto.SalesOrderId.Value, cancellationToken);
            if (linkedOrder != null)
            {
                linkedOrder.Status = OrderStatus.Delivered;
            }
        }

        _context.SalesInvoices.Add(invoice);
        await _context.SaveChangesAsync(cancellationToken);

        return await GetSalesInvoiceByIdAsync(invoice.Id, cancellationToken);
    }

    public async Task<ApiResponse<bool>> CancelSalesInvoiceAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var invoice = await _context.SalesInvoices
            .Include(i => i.Items)
            .Include(i => i.Customer)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

        if (invoice == null)
        {
            throw new NotFoundException("SalesInvoice", id);
        }

        if (invoice.PaidAmount > 0)
        {
            throw new BusinessRuleException("Cannot cancel an invoice that has payments recorded. Reverse payments first.");
        }

        if (invoice.Status == InvoiceStatus.Cancelled)
        {
            throw new BusinessRuleException("Invoice is already cancelled.");
        }

        var now = _dateTimeProvider.UtcNow;
        var userEmail = _currentUserService.Email ?? "System";

        // Reverse stock deductions
        foreach (var item in invoice.Items)
        {
            var stock = await _context.InventoryStocks
                .FirstOrDefaultAsync(s => s.ProductId == item.ProductId && s.WarehouseId == invoice.WarehouseId, cancellationToken);

            if (stock != null)
            {
                stock.QuantityOnHand += item.Quantity;
                stock.UpdatedOn = now;
                stock.UpdatedBy = userEmail;

                _context.StockTransactions.Add(new StockTransaction
                {
                    Id = Guid.NewGuid(),
                    BusinessId = invoice.BusinessId,
                    ProductId = item.ProductId,
                    WarehouseId = invoice.WarehouseId,
                    TransactionType = StockTransactionType.Inward,
                    Quantity = item.Quantity,
                    UnitPrice = stock.AverageCost,
                    TotalAmount = item.Quantity * stock.AverageCost,
                    ReferenceType = "SalesInvoiceCancel",
                    ReferenceId = $"CANCEL-{invoice.InvoiceNumber}",
                    Notes = $"Restocked due to invoice cancellation: {invoice.InvoiceNumber}",
                    Timestamp = now,
                    CreatedOn = now,
                    CreatedBy = userEmail
                });
            }
        }

        // Reduce customer balance
        if (invoice.Customer != null)
        {
            invoice.Customer.OutstandingBalance = Math.Max(0, invoice.Customer.OutstandingBalance - invoice.BalanceAmount);
        }

        invoice.Status = InvoiceStatus.Cancelled;
        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.SuccessResult(true, "Invoice cancelled and inventory restocked successfully.");
    }

    // ==========================================
    // PAYMENTS
    // ==========================================

    public async Task<ApiResponse<SalesPaymentDto>> RecordPaymentAsync(RecordSalesPaymentDto dto, CancellationToken cancellationToken = default)
    {
        var invoice = await _context.SalesInvoices
            .Include(i => i.Customer)
            .FirstOrDefaultAsync(i => i.Id == dto.SalesInvoiceId, cancellationToken);

        if (invoice == null)
        {
            throw new NotFoundException("SalesInvoice", dto.SalesInvoiceId);
        }

        if (invoice.Status == InvoiceStatus.Paid)
        {
            throw new BusinessRuleException("Invoice is already fully settled.");
        }

        if (invoice.Status == InvoiceStatus.Cancelled)
        {
            throw new BusinessRuleException("Cannot record payment against a cancelled invoice.");
        }

        if (dto.Amount > invoice.BalanceAmount)
        {
            throw new BusinessRuleException($"Payment amount ₹{dto.Amount:N2} exceeds remaining invoice balance of ₹{invoice.BalanceAmount:N2}.");
        }

        var count = await _context.SalesPayments.CountAsync(p => p.BusinessId == invoice.BusinessId, cancellationToken);
        var paymentNumber = $"PAY-{_dateTimeProvider.UtcNow.Year}-{(count + 1):D4}";

        var payment = new SalesPayment
        {
            Id = Guid.NewGuid(),
            BusinessId = invoice.BusinessId,
            PaymentNumber = paymentNumber,
            SalesInvoiceId = invoice.Id,
            CustomerId = invoice.CustomerId,
            PaymentDate = dto.PaymentDate,
            Amount = dto.Amount,
            Method = dto.Method,
            ReferenceNumber = dto.ReferenceNumber,
            Notes = dto.Notes
        };

        invoice.PaidAmount += dto.Amount;
        invoice.BalanceAmount -= dto.Amount;
        invoice.Status = invoice.BalanceAmount == 0m ? InvoiceStatus.Paid : InvoiceStatus.PartiallyPaid;

        if (invoice.Customer != null)
        {
            invoice.Customer.OutstandingBalance = Math.Max(0m, invoice.Customer.OutstandingBalance - dto.Amount);
        }

        _context.SalesPayments.Add(payment);
        await _context.SaveChangesAsync(cancellationToken);

        var resultDto = new SalesPaymentDto
        {
            Id = payment.Id,
            PaymentNumber = payment.PaymentNumber,
            SalesInvoiceId = payment.SalesInvoiceId,
            InvoiceNumber = invoice.InvoiceNumber,
            CustomerId = payment.CustomerId,
            CustomerName = invoice.Customer?.Name ?? string.Empty,
            PaymentDate = payment.PaymentDate,
            Amount = payment.Amount,
            Method = payment.Method,
            ReferenceNumber = payment.ReferenceNumber,
            Notes = payment.Notes,
            CreatedOn = payment.CreatedOn.UtcDateTime
        };

        return ApiResponse<SalesPaymentDto>.SuccessResult(resultDto, "Payment recorded successfully.");
    }

    // ==========================================
    // FINANCIAL SUMMARY
    // ==========================================

    public async Task<ApiResponse<SalesSummaryDto>> GetSalesSummaryAsync(CancellationToken cancellationToken = default)
    {
        var now = _dateTimeProvider.UtcNow;

        var activeInvoices = await _context.SalesInvoices
            .AsNoTracking()
            .Where(i => i.Status != InvoiceStatus.Cancelled)
            .ToListAsync(cancellationToken);

        var totalSales = activeInvoices.Sum(i => i.TotalAmount);
        var totalPaid = activeInvoices.Sum(i => i.PaidAmount);
        var totalOutstanding = activeInvoices.Sum(i => i.BalanceAmount);
        var totalOverdue = activeInvoices
            .Where(i => i.BalanceAmount > 0 && i.DueDate < now)
            .Sum(i => i.BalanceAmount);

        var pendingOrdersCount = await _context.SalesOrders
            .AsNoTracking()
            .CountAsync(o => o.Status == OrderStatus.Draft || o.Status == OrderStatus.Confirmed, cancellationToken);

        var activeCustomersCount = await _context.Customers
            .AsNoTracking()
            .CountAsync(c => c.IsActive, cancellationToken);

        var summary = new SalesSummaryDto
        {
            TotalSales = totalSales,
            TotalPaid = totalPaid,
            TotalOutstanding = totalOutstanding,
            TotalOverdue = totalOverdue,
            TotalInvoicesCount = activeInvoices.Count,
            PendingOrdersCount = pendingOrdersCount,
            ActiveCustomersCount = activeCustomersCount
        };

        return ApiResponse<SalesSummaryDto>.SuccessResult(summary);
    }
}
