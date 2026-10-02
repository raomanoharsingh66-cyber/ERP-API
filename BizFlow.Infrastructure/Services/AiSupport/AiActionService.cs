using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.DTOs.AiSupport;
using BizFlow.Application.DTOs.Purchases;
using BizFlow.Application.DTOs.Inventory;
using BizFlow.Domain.Constants;
using BizFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BizFlow.Infrastructure.Services.AiSupport;

public class AiActionService : IAiActionService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPurchaseService _purchaseService;
    private readonly IInventoryService _inventoryService;
    private readonly IAiAuditService _auditService;
    private readonly ILogger<AiActionService> _logger;

    public AiActionService(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IPurchaseService purchaseService,
        IInventoryService inventoryService,
        IAiAuditService auditService,
        ILogger<AiActionService> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _purchaseService = purchaseService;
        _inventoryService = inventoryService;
        _auditService = auditService;
        _logger = logger;
    }

    private Guid RequireBusinessId()
    {
        if (_currentUserService.BusinessId.HasValue)
        {
            return _currentUserService.BusinessId.Value;
        }

        var defaultBiz = _context.Businesses.OrderBy(b => b.CreatedOn).FirstOrDefault();
        if (defaultBiz != null)
        {
            return defaultBiz.Id;
        }

        throw new BusinessRuleException("A valid business context is required for AI Action execution.");
    }

    public async Task<AiProposedActionDto?> PlanActionAsync(AiSupportContextDto context, string intent, CancellationToken cancellationToken = default)
    {
        var lowerIntent = intent.ToLower();

        // 1. Plan Product Activation (Self-Healing Fix)
        if (lowerIntent.Contains("activate") || lowerIntent.Contains("enable product"))
        {
            if (!string.IsNullOrEmpty(context.SelectedProductId) && Guid.TryParse(context.SelectedProductId, out var prodId))
            {
                var prod = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == prodId, cancellationToken);
                if (prod != null)
                {
                    return new AiProposedActionDto
                    {
                        ActionType = "ActivateProduct",
                        Title = $"Activate Product: {prod.Name}",
                        Description = $"Reactivate SKU {prod.SKU} so it can be added to sales orders and purchase orders.",
                        RiskLevel = "RequiresConfirmation",
                        Reason = "Product is currently marked as Inactive in the catalog.",
                        RequiredPermission = Permissions.Inventory.Update,
                        UserHasPermission = _currentUserService.Permissions.Contains(Permissions.Inventory.Update) || _currentUserService.IsSuperAdmin,
                        PreviewTable = new List<AiActionPreviewItemDto>
                        {
                            new() { Field = "Product Name", Value = prod.Name },
                            new() { Field = "SKU", Value = prod.SKU },
                            new() { Field = "Proposed Status", Value = "Active (True)" }
                        },
                        Payload = new Dictionary<string, string>
                        {
                            ["productId"] = prod.Id.ToString()
                        }
                    };
                }
            }
        }

        // 2. Plan Purchase Request / Order Creation
        if (lowerIntent.Contains("purchase") || lowerIntent.Contains("reorder") || lowerIntent.Contains("buy") || lowerIntent.Contains("order stock"))
        {
            Product? targetProduct = null;
            if (!string.IsNullOrEmpty(context.SelectedProductId) && Guid.TryParse(context.SelectedProductId, out var pId))
            {
                targetProduct = await _context.Products.AsNoTracking().Include(p => p.UnitOfMeasure).FirstOrDefaultAsync(p => p.Id == pId, cancellationToken);
            }

            if (targetProduct != null)
            {
                var reqQty = context.RequestedQuantity ?? Math.Max(targetProduct.MinStockLevel, 50m);
                var warehouse = await _context.Warehouses.AsNoTracking().OrderBy(w => w.CreatedOn).FirstOrDefaultAsync(cancellationToken);
                var supplier = await _context.Suppliers.AsNoTracking().OrderBy(s => s.CreatedOn).FirstOrDefaultAsync(cancellationToken);

                return new AiProposedActionDto
                {
                    ActionType = "CreatePurchaseRequest",
                    Title = $"Draft Purchase Order for {targetProduct.Name}",
                    Description = $"Generate an official Purchase Order for {reqQty} {targetProduct.UnitOfMeasure?.Code ?? "units"} to replenish warehouse stock.",
                    RiskLevel = "RequiresConfirmation",
                    Reason = "Warehouse stock is below required consumption threshold.",
                    RequiredPermission = Permissions.Purchases.Create,
                    UserHasPermission = _currentUserService.Permissions.Contains(Permissions.Purchases.Create) || _currentUserService.IsSuperAdmin,
                    PreviewTable = new List<AiActionPreviewItemDto>
                    {
                        new() { Field = "Product", Value = targetProduct.Name },
                        new() { Field = "SKU", Value = targetProduct.SKU },
                        new() { Field = "Quantity", Value = $"{reqQty} {targetProduct.UnitOfMeasure?.Code ?? "units"}" },
                        new() { Field = "Est. Unit Price", Value = $"₹{targetProduct.PurchasePrice:N2}" },
                        new() { Field = "Est. Total Amount", Value = $"₹{(reqQty * targetProduct.PurchasePrice):N2}" },
                        new() { Field = "Target Warehouse", Value = warehouse?.Name ?? "Main Central Warehouse" },
                        new() { Field = "Vendor / Supplier", Value = supplier?.Name ?? "Primary Approved Vendor" }
                    },
                    Payload = new Dictionary<string, string>
                    {
                        ["productId"] = targetProduct.Id.ToString(),
                        ["quantity"] = reqQty.ToString(),
                        ["unitPrice"] = targetProduct.PurchasePrice.ToString(),
                        ["warehouseId"] = warehouse?.Id.ToString() ?? Guid.Empty.ToString(),
                        ["supplierId"] = supplier?.Id.ToString() ?? Guid.Empty.ToString(),
                        ["notes"] = "AI Guided Support Requisition"
                    }
                };
            }
        }

        // 3. Plan Stock Adjustment
        if (lowerIntent.Contains("adjust stock") || lowerIntent.Contains("correct stock"))
        {
            if (!string.IsNullOrEmpty(context.SelectedProductId) && Guid.TryParse(context.SelectedProductId, out var adjProdId))
            {
                var prod = await _context.Products.AsNoTracking().Include(p => p.UnitOfMeasure).FirstOrDefaultAsync(p => p.Id == adjProdId, cancellationToken);
                var warehouse = await _context.Warehouses.AsNoTracking().FirstOrDefaultAsync(cancellationToken);

                return new AiProposedActionDto
                {
                    ActionType = "AdjustStock",
                    Title = $"Stock Count Adjustment for {prod?.Name ?? "Item"}",
                    Description = "Adjust system inventory count to reconcile with physical shelf audit.",
                    RiskLevel = "RequiresConfirmation",
                    Reason = "Physical stock difference reported.",
                    RequiredPermission = Permissions.Inventory.AdjustStock,
                    UserHasPermission = _currentUserService.Permissions.Contains(Permissions.Inventory.AdjustStock) || _currentUserService.IsSuperAdmin,
                    PreviewTable = new List<AiActionPreviewItemDto>
                    {
                        new() { Field = "Product", Value = prod?.Name ?? "Product" },
                        new() { Field = "Warehouse", Value = warehouse?.Name ?? "Central Store" },
                        new() { Field = "Adjustment Type", Value = "Reconciliation Correction" }
                    },
                    Payload = new Dictionary<string, string>
                    {
                        ["productId"] = prod?.Id.ToString() ?? string.Empty,
                        ["warehouseId"] = warehouse?.Id.ToString() ?? string.Empty
                    }
                };
            }
        }

        return null;
    }

    public async Task<AiExecuteActionResponseDto> ExecuteConfirmedActionAsync(AiExecuteActionRequestDto request, CancellationToken cancellationToken = default)
    {
        var businessId = RequireBusinessId();
        var userId = _currentUserService.UserId ?? Guid.Empty;

        // Mandatory check 1: User Confirmation
        if (!request.UserConfirmed)
        {
            return new AiExecuteActionResponseDto
            {
                Success = false,
                Message = "Action aborted: Explicit user confirmation was not provided.",
                ActionType = request.ActionType
            };
        }

        try
        {
            switch (request.ActionType)
            {
                // Action 1: Self-healing Activate Product
                case "ActivateProduct":
                {
                    if (!_currentUserService.Permissions.Contains(Permissions.Inventory.Update) && !_currentUserService.IsSuperAdmin)
                    {
                        throw new UnauthorizedAccessException("Missing required permission: 'Inventory.Update'");
                    }

                    if (!request.Payload.TryGetValue("productId", out var pIdStr) || !Guid.TryParse(pIdStr, out var prodId))
                    {
                        throw new BusinessRuleException("Valid Product ID is required for activation.");
                    }

                    var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == prodId && p.BusinessId == businessId, cancellationToken);
                    if (product == null)
                    {
                        throw new NotFoundException(nameof(Product), prodId);
                    }

                    product.IsActive = true;
                    await _context.SaveChangesAsync(cancellationToken);

                    var auditId = await _auditService.LogAiActionAsync(
                        userId, businessId, request.SessionId,
                        "User confirmed activation of product", "ActivateProduct",
                        JsonSerializer.Serialize(request.Payload),
                        true, true, $"Product {product.Name} (SKU: {product.SKU}) activated successfully.",
                        product.Id.ToString(), cancellationToken);

                    return new AiExecuteActionResponseDto
                    {
                        Success = true,
                        Message = $"Product '{product.Name}' has been successfully activated! You can now use it in invoices and orders.",
                        ActionType = request.ActionType,
                        RecordId = product.Id.ToString(),
                        AuditLogId = auditId,
                        RedirectRoute = "/inventory/products"
                    };
                }

                // Action 2: Controlled Purchase Order Creation
                case "CreatePurchaseRequest":
                {
                    if (!_currentUserService.Permissions.Contains(Permissions.Purchases.Create) && !_currentUserService.IsSuperAdmin)
                    {
                        throw new UnauthorizedAccessException("Missing required permission: 'Purchases.Create'");
                    }

                    if (!request.Payload.TryGetValue("productId", out var pIdStr) || !Guid.TryParse(pIdStr, out var prodId) ||
                        !request.Payload.TryGetValue("quantity", out var qtyStr) || !decimal.TryParse(qtyStr, out var qty) ||
                        !request.Payload.TryGetValue("warehouseId", out var whIdStr) || !Guid.TryParse(whIdStr, out var whId) ||
                        !request.Payload.TryGetValue("supplierId", out var suppIdStr) || !Guid.TryParse(suppIdStr, out var suppId))
                    {
                        throw new BusinessRuleException("Incomplete parameters for Purchase Order creation.");
                    }

                    decimal unitPrice = 0m;
                    if (request.Payload.TryGetValue("unitPrice", out var priceStr) && decimal.TryParse(priceStr, out var p))
                    {
                        unitPrice = p;
                    }

                    var poDto = new CreatePurchaseOrderDto
                    {
                        SupplierId = suppId,
                        WarehouseId = whId,
                        OrderDate = DateTime.UtcNow,
                        ExpectedDeliveryDate = DateTime.UtcNow.AddDays(7),
                        Notes = request.Payload.GetValueOrDefault("notes", "AI Assistant Automated Purchase Order"),
                        Items = new List<CreatePurchaseOrderItemDto>
                        {
                            new()
                            {
                                ProductId = prodId,
                                OrderedQuantity = qty,
                                UnitPrice = unitPrice,
                                TaxRate = 18m,
                                Notes = "AI Automated Reorder Line"
                            }
                        }
                    };

                    var poResult = await _purchaseService.CreatePurchaseOrderAsync(poDto, cancellationToken);
                    if (!poResult.Success || poResult.Data == null)
                    {
                        throw new BusinessRuleException(poResult.Message ?? "Failed to create purchase order through domain service.");
                    }

                    var auditId = await _auditService.LogAiActionAsync(
                        userId, businessId, request.SessionId,
                        "User confirmed PO creation", "CreatePurchaseRequest",
                        JsonSerializer.Serialize(request.Payload),
                        true, true, $"Purchase Order {poResult.Data.PONumber} created for Product {prodId} (Qty: {qty}).",
                        poResult.Data.Id.ToString(), cancellationToken);

                    return new AiExecuteActionResponseDto
                    {
                        Success = true,
                        Message = $"Purchase Order #{poResult.Data.PONumber} has been created successfully for {qty} units! It is currently in Draft status ready for approval.",
                        ActionType = request.ActionType,
                        RecordId = poResult.Data.Id.ToString(),
                        AuditLogId = auditId,
                        RedirectRoute = "/purchases/orders"
                    };
                }

                default:
                    throw new BusinessRuleException($"Unsupported or unverified AI action type: '{request.ActionType}'");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute AI Action: {ActionType}", request.ActionType);

            await _auditService.LogAiActionAsync(
                userId, businessId, request.SessionId,
                "Action execution failed", request.ActionType,
                JsonSerializer.Serialize(request.Payload),
                true, false, ex.Message, null, cancellationToken);

            return new AiExecuteActionResponseDto
            {
                Success = false,
                Message = $"Action rejected: {ex.Message}",
                ActionType = request.ActionType,
                FailedDetails = "The business layer rejected this operation. Check permissions, active state, and validation rules."
            };
        }
    }
}
