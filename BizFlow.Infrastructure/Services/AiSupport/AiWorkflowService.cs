using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.DTOs.AiSupport;
using BizFlow.Domain.Constants;
using BizFlow.Domain.Entities;
using BizFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BizFlow.Infrastructure.Services.AiSupport;

public class AiWorkflowService : IAiWorkflowService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public AiWorkflowService(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    private Guid? CurrentBusinessId => _currentUserService.BusinessId;

    public async Task<WorkflowGuidanceDto> EvaluateWorkflowAsync(AiSupportContextDto context, CancellationToken cancellationToken = default)
    {
        var route = (context.Route ?? string.Empty).ToLower();
        var module = !string.IsNullOrEmpty(context.Module) ? context.Module : "General";

        // 1. Purchases Workflow
        if (route.Contains("/purchases") || module.Equals("Purchases", StringComparison.OrdinalIgnoreCase))
        {
            return await EvaluatePurchaseWorkflowAsync(context, cancellationToken);
        }

        // 2. Sales Workflow
        if (route.Contains("/sales") || module.Equals("Sales", StringComparison.OrdinalIgnoreCase))
        {
            return await EvaluateSalesWorkflowAsync(context, cancellationToken);
        }

        // 3. Inventory Workflow
        if (route.Contains("/inventory") || module.Equals("Inventory", StringComparison.OrdinalIgnoreCase))
        {
            return await EvaluateInventoryWorkflowAsync(context, cancellationToken);
        }

        // Default General Workflow Guidance
        return new WorkflowGuidanceDto
        {
            Module = module,
            CurrentStage = context.Page ?? "Dashboard Overview",
            CurrentStageDescription = "You are currently in the main ERP workspace.",
            CompletedStages = new List<string> { "Authentication", "Tenant Context Verified" },
            NextStage = "Select Desired Operational Module",
            NextStageDescription = "Navigate to Sales, Purchases, or Inventory to begin a transaction.",
            AllowedNextActions = new List<WorkflowActionOptionDto>
            {
                new() { Title = "Create Sales Invoice", ActionCode = "NAV_SALES_CREATE", Description = "Bill products to a customer", TargetRoute = "/sales/create", RequiredPermission = Permissions.Sales.Create },
                new() { Title = "Smart Purchase Assistant", ActionCode = "NAV_AI_PURCHASE", Description = "Review AI reorder recommendations", TargetRoute = "/ai-assistant/smart-purchase", RequiredPermission = Permissions.Purchases.View },
                new() { Title = "Product Catalog", ActionCode = "NAV_INVENTORY", Description = "Check warehouse stock levels", TargetRoute = "/inventory/products", RequiredPermission = Permissions.Inventory.View }
            }
        };
    }

    private async Task<WorkflowGuidanceDto> EvaluatePurchaseWorkflowAsync(AiSupportContextDto context, CancellationToken cancellationToken)
    {
        PurchaseOrder? po = null;
        if (!string.IsNullOrEmpty(context.RecordId) && Guid.TryParse(context.RecordId, out var poId))
        {
            po = await _context.PurchaseOrders
                .AsNoTracking()
                .Include(p => p.Items)
                .Include(p => p.Supplier)
                .Include(p => p.Warehouse)
                .FirstOrDefaultAsync(p => p.Id == poId, cancellationToken);
        }

        // If on PO creation page
        if ((context.Route ?? string.Empty).Contains("/purchases/orders") && po == null)
        {
            return new WorkflowGuidanceDto
            {
                Module = "Purchases",
                CurrentStage = "Purchase Order Management",
                CurrentStageDescription = "Viewing vendor procurement orders and goods inward logs.",
                CompletedStages = new List<string> { "Reorder Planning" },
                NextStage = "Generate Purchase Order or Receive Goods",
                NextStageDescription = "Create a new Purchase Order for suppliers or record Goods Receipt Notes (GRN) for arriving shipments.",
                AllowedNextActions = new List<WorkflowActionOptionDto>
                {
                    new() { Title = "Create Purchase Order", ActionCode = "CREATE_PO", Description = "Raise official order with vendor", TargetRoute = "/purchases/orders", RequiredPermission = Permissions.Purchases.Create },
                    new() { Title = "View Vendor Bills", ActionCode = "NAV_BILLS", Description = "Check unpaid vendor payables", TargetRoute = "/purchases/bills", RequiredPermission = Permissions.Purchases.View },
                    new() { Title = "Smart Reorder Assistant", ActionCode = "NAV_AI_PURCHASE", Description = "View automated low-stock reorder suggestions", TargetRoute = "/ai-assistant/smart-purchase", RequiredPermission = Permissions.Purchases.View }
                }
            };
        }

        if (po != null)
        {
            var userHasApprove = _currentUserService.Permissions.Contains(Permissions.Purchases.Approve) || _currentUserService.IsSuperAdmin;
            var poNum = po.PONumber;

            switch (po.Status)
            {
                case PurchaseOrderStatus.Draft:
                    return new WorkflowGuidanceDto
                    {
                        Module = "Purchases",
                        CurrentStage = "Purchase Order Draft",
                        CurrentStageDescription = $"Order {poNum} has been drafted with {po.Items.Count} line items totaling ₹{po.TotalAmount:N2}.",
                        CompletedStages = new List<string> { "PO Draft Created" },
                        NextStage = "Manager Approval",
                        NextStageDescription = userHasApprove
                            ? "Your role has permission to Approve this order. Click 'Approve' to confirm and release to vendor."
                            : "This order is waiting for approval by a manager with 'Purchases.Approve' permission.",
                        AllowedNextActions = new List<WorkflowActionOptionDto>
                        {
                            new() { Title = "Approve Order", ActionCode = "APPROVE_PO", Description = "Confirm procurement order", RequiredPermission = Permissions.Purchases.Approve, CanExecute = userHasApprove },
                            new() { Title = "Print PO Copy", ActionCode = "PRINT_PO", Description = "Preview official purchase order sheet" }
                        },
                        IsBlocked = !userHasApprove,
                        BlockReason = !userHasApprove ? "Approval requires 'Purchases.Approve' role permission." : null
                    };

                case PurchaseOrderStatus.Approved:
                    return new WorkflowGuidanceDto
                    {
                        Module = "Purchases",
                        CurrentStage = "Purchase Order Approved",
                        CurrentStageDescription = $"Order {poNum} is approved and pending physical shipment delivery from {po.Supplier?.Name ?? "Vendor"}.",
                        CompletedStages = new List<string> { "PO Draft Created", "Manager Approved" },
                        NextStage = "Goods Receipt Note (GRN)",
                        NextStageDescription = "Your Purchase Order has been approved. The next step is to create a Goods Receipt Note (GRN) when the material is received at the warehouse.",
                        AllowedNextActions = new List<WorkflowActionOptionDto>
                        {
                            new() { Title = "Receive Goods (GRN)", ActionCode = "RECEIVE_GRN", Description = "Record arrival and credit warehouse stock", RequiredPermission = Permissions.Purchases.Update },
                            new() { Title = "Print Purchase Order", ActionCode = "PRINT_PO", Description = "Download official PO document" }
                        }
                    };

                case PurchaseOrderStatus.PartiallyReceived:
                    return new WorkflowGuidanceDto
                    {
                        Module = "Purchases",
                        CurrentStage = "Partially Received",
                        CurrentStageDescription = $"Merchandise for {poNum} has arrived in part. Physical inventory has been credited for accepted lines.",
                        CompletedStages = new List<string> { "PO Created", "Approved", "Initial GRN Inwarded" },
                        NextStage = "Receive Remaining Balance or Record Bill",
                        NextStageDescription = "Log an additional GRN when the remaining quantity arrives, or generate a Vendor Bill for delivered items.",
                        AllowedNextActions = new List<WorkflowActionOptionDto>
                        {
                            new() { Title = "Receive Remaining GRN", ActionCode = "RECEIVE_GRN", Description = "Inward second delivery batch", RequiredPermission = Permissions.Purchases.Update },
                            new() { Title = "Create Vendor Bill", ActionCode = "CREATE_BILL", Description = "Record AP invoice against received goods", TargetRoute = "/purchases/bills", RequiredPermission = Permissions.Purchases.Create }
                        }
                    };

                case PurchaseOrderStatus.Received:
                    return new WorkflowGuidanceDto
                    {
                        Module = "Purchases",
                        CurrentStage = "Goods Fully Received",
                        CurrentStageDescription = $"All ordered items for {poNum} have been received and verified into {po.Warehouse?.Name ?? "warehouse"}.",
                        CompletedStages = new List<string> { "PO Created", "Approved", "Goods Receipt Verified", "Warehouse Stock Credited" },
                        NextStage = "Record Vendor Bill (Accounts Payable)",
                        NextStageDescription = "Procurement delivery is complete. The next financial step is recording the vendor's tax bill for payment processing.",
                        AllowedNextActions = new List<WorkflowActionOptionDto>
                        {
                            new() { Title = "Generate Vendor Bill", ActionCode = "CREATE_BILL", Description = "Record bill in Accounts Payable", TargetRoute = "/purchases/bills", RequiredPermission = Permissions.Purchases.Create },
                            new() { Title = "View Inward GRNs", ActionCode = "VIEW_GRN", Description = "Review inspection and quality notes" }
                        }
                    };
            }
        }

        return new WorkflowGuidanceDto
        {
            Module = "Purchases",
            CurrentStage = "Procurement Flow",
            CurrentStageDescription = "Active procurement process.",
            NextStage = "Review Pending Orders",
            AllowedNextActions = new List<WorkflowActionOptionDto>()
        };
    }

    private async Task<WorkflowGuidanceDto> EvaluateSalesWorkflowAsync(AiSupportContextDto context, CancellationToken cancellationToken)
    {
        var route = (context.Route ?? string.Empty).ToLower();

        // Check if on Create Invoice screen
        if (route.Contains("/sales/create"))
        {
            bool isBlocked = false;
            string? blockReason = null;
            var actions = new List<WorkflowActionOptionDto>();

            // Stock Check if product and warehouse specified
            if (!string.IsNullOrEmpty(context.SelectedProductId) && Guid.TryParse(context.SelectedProductId, out var prodId))
            {
                var product = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == prodId, cancellationToken);
                var reqQty = context.RequestedQuantity ?? 1m;

                Guid? whId = null;
                if (!string.IsNullOrEmpty(context.SelectedWarehouseId) && Guid.TryParse(context.SelectedWarehouseId, out var parsedWhId))
                {
                    whId = parsedWhId;
                }

                var stockQuery = _context.InventoryStocks.AsNoTracking().Where(s => s.ProductId == prodId);
                if (whId.HasValue)
                {
                    stockQuery = stockQuery.Where(s => s.WarehouseId == whId.Value);
                }

                var totalAvail = await stockQuery.SumAsync(s => (decimal?)(s.QuantityOnHand - s.QuantityReserved), cancellationToken) ?? 0m;

                if (totalAvail < reqQty)
                {
                    isBlocked = true;
                    blockReason = $"Insufficient stock for {product?.Name ?? "selected product"}. Required: {reqQty}, Available in selected warehouse: {totalAvail}.";

                    actions.Add(new WorkflowActionOptionDto
                    {
                        Title = "Create Purchase Request",
                        ActionCode = "PROPOSE_PURCHASE_ORDER",
                        Description = $"Raise purchase reorder for shortage of {(reqQty - totalAvail)} {product?.UnitOfMeasure?.Code ?? "units"}.",
                        RequiredPermission = Permissions.Purchases.Create
                    });

                    actions.Add(new WorkflowActionOptionDto
                    {
                        Title = "Check Warehouse Stocks",
                        ActionCode = "NAV_STOCK",
                        Description = "Inspect stock balances in other company warehouses",
                        TargetRoute = "/inventory/stock",
                        RequiredPermission = Permissions.Inventory.View
                    });
                }
            }

            if (!isBlocked)
            {
                actions.Add(new WorkflowActionOptionDto
                {
                    Title = "Save & Issue Invoice",
                    ActionCode = "SUBMIT_INVOICE",
                    Description = "Confirm billing, debit physical inventory, and register Accounts Receivable",
                    RequiredPermission = Permissions.Sales.Create
                });
            }

            return new WorkflowGuidanceDto
            {
                Module = "Sales",
                CurrentStage = "Sales Invoice Creation",
                CurrentStageDescription = "Drafting tax invoice for customer dispatch.",
                CompletedStages = new List<string> { "Customer Selected", "Warehouse Selected" },
                NextStage = isBlocked ? "Resolve Inventory Shortage" : "Review Tax Totals and Issue",
                NextStageDescription = isBlocked
                    ? blockReason!
                    : "Review product line items, GST rate determination, and click Save to issue invoice.",
                AllowedNextActions = actions,
                IsBlocked = isBlocked,
                BlockReason = blockReason,
                Prerequisites = new List<string> { "Active Customer", "Sufficient Warehouse Stock", "Sales.Create Permission" }
            };
        }

        // Default Sales Guidance
        return new WorkflowGuidanceDto
        {
            Module = "Sales",
            CurrentStage = "Sales Invoices Directory",
            CurrentStageDescription = "Tracking issued invoices, payments, and receivables.",
            CompletedStages = new List<string> { "Order Invoicing" },
            NextStage = "Issue New Invoice or Record Payment",
            NextStageDescription = "Select an existing unpaid invoice to record customer payment receipt or create a new invoice.",
            AllowedNextActions = new List<WorkflowActionOptionDto>
            {
                new() { Title = "Create Sales Invoice", ActionCode = "CREATE_INVOICE", Description = "Issue new GST invoice", TargetRoute = "/sales/create", RequiredPermission = Permissions.Sales.Create },
                new() { Title = "Customer Directory", ActionCode = "NAV_CUSTOMERS", Description = "Manage customer credit terms and balances", TargetRoute = "/sales/customers", RequiredPermission = Permissions.Customers.View }
            }
        };
    }

    private Task<WorkflowGuidanceDto> EvaluateInventoryWorkflowAsync(AiSupportContextDto context, CancellationToken cancellationToken)
    {
        return Task.FromResult(new WorkflowGuidanceDto
        {
            Module = "Inventory",
            CurrentStage = "Merchandise & Stock Level Control",
            CurrentStageDescription = "Managing SKUs, pricing, GST classifications, and multi-warehouse balances.",
            CompletedStages = new List<string> { "Catalog Setup" },
            NextStage = "Audit Stock or Review Reorder Levels",
            NextStageDescription = "Verify physical quantities on hand against system balances or check AI purchase recommendations.",
            AllowedNextActions = new List<WorkflowActionOptionDto>
            {
                new() { Title = "Smart Purchase Assistant", ActionCode = "NAV_AI_PURCHASE", Description = "View automated low-stock reorder suggestions", TargetRoute = "/ai-assistant/smart-purchase", RequiredPermission = Permissions.Purchases.View },
                new() { Title = "Physical Stock Adjustment", ActionCode = "ADJUST_STOCK", Description = "Adjust physical inventory count differences", TargetRoute = "/inventory/stock", RequiredPermission = Permissions.Inventory.AdjustStock },
                new() { Title = "Warehouse Stock View", ActionCode = "NAV_STOCK", Description = "Breakdown by warehouse location", TargetRoute = "/inventory/stock", RequiredPermission = Permissions.Inventory.View }
            }
        });
    }
}
