using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.DTOs.AiSupport;
using BizFlow.Domain.Constants;
using Microsoft.EntityFrameworkCore;

namespace BizFlow.Infrastructure.Services.AiSupport;

public class AiTroubleshootingService : IAiTroubleshootingService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public AiTroubleshootingService(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<RootCauseAnalysisDto> DiagnoseIssueAsync(AiSupportContextDto context, CancellationToken cancellationToken = default)
    {
        var errMsg = (context.ErrorMessage ?? string.Empty).ToLower();
        var page = (context.Page ?? string.Empty).ToLower();
        var route = (context.Route ?? string.Empty).ToLower();

        // 1. Diagnose Insufficient Stock Issue
        if (errMsg.Contains("stock") || errMsg.Contains("insufficient") || errMsg.Contains("quantityonhand") ||
            (context.RequestedQuantity.HasValue && !string.IsNullOrEmpty(context.SelectedProductId)))
        {
            if (Guid.TryParse(context.SelectedProductId, out var prodId))
            {
                var prod = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == prodId, cancellationToken);
                var stocks = await _context.InventoryStocks.AsNoTracking().Include(s => s.Warehouse).Where(s => s.ProductId == prodId).ToListAsync(cancellationToken);
                var totalAvail = stocks.Sum(s => s.QuantityOnHand - s.QuantityReserved);
                var req = context.RequestedQuantity ?? 1m;

                if (totalAvail < req)
                {
                    return new RootCauseAnalysisDto
                    {
                        IssueSummary = $"Insufficient stock for {prod?.Name ?? "item"} in warehouse.",
                        RootCauseCategory = "StockInsufficient",
                        Detail = $"Requested: {req} {prod?.UnitOfMeasure?.Code ?? "units"}, but only {totalAvail} {prod?.UnitOfMeasure?.Code ?? "units"} is available on hand across warehouses (Reserved: {stocks.Sum(s => s.QuantityReserved)}).",
                        RequiredPermission = Permissions.Purchases.Create,
                        MissingPrerequisite = $"Reorder shortage of {req - totalAvail} {prod?.UnitOfMeasure?.Code ?? "units"}",
                        RecommendedResolution = "Create a Purchase Order to replenish warehouse stock or perform an internal stock transfer from a surplus warehouse location.",
                        CanAutoFix = true
                    };
                }
            }
        }

        // 2. Diagnose Inactive Product Issue
        if (!string.IsNullOrEmpty(context.SelectedProductId) && Guid.TryParse(context.SelectedProductId, out var checkProdId))
        {
            var prod = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == checkProdId, cancellationToken);
            if (prod != null && !prod.IsActive)
            {
                var canEdit = _currentUserService.Permissions.Contains(Permissions.Inventory.Update) || _currentUserService.IsSuperAdmin;
                return new RootCauseAnalysisDto
                {
                    IssueSummary = $"Product '{prod.Name}' is marked Inactive.",
                    RootCauseCategory = "InactiveEntity",
                    Detail = $"The SKU '{prod.SKU}' has been archived or disabled. Inactive items cannot be added to sales invoices or purchase orders.",
                    RequiredPermission = Permissions.Inventory.Update,
                    RecommendedResolution = canEdit
                        ? "Activate this product to resume transactions."
                        : "Ask your inventory administrator to activate this SKU.",
                    CanAutoFix = canEdit
                };
            }
        }

        // 3. Diagnose Permission Issues
        if (errMsg.Contains("forbidden") || errMsg.Contains("permission") || errMsg.Contains("unauthorized") || context.HttpStatus == 403)
        {
            string reqPerm = "Operational Permission";
            if (route.Contains("/purchases")) reqPerm = Permissions.Purchases.Approve;
            else if (route.Contains("/inventory")) reqPerm = Permissions.Inventory.AdjustStock;
            else if (route.Contains("/sales")) reqPerm = Permissions.Sales.Create;
            else if (route.Contains("/settings/users")) reqPerm = Permissions.Users.Create;

            return new RootCauseAnalysisDto
            {
                IssueSummary = "Your user role lacks the required authorization.",
                RootCauseCategory = "PermissionMissing",
                Detail = $"Current user ({_currentUserService.Email ?? "Account"}) does not possess the '{reqPerm}' permission required to complete this action.",
                RequiredPermission = reqPerm,
                RecommendedResolution = "Contact your enterprise administrator in Settings -> Roles & Security to assign the required permission to your role.",
                CanAutoFix = false
            };
        }

        // 4. Diagnose Null / Mandatory Field Constraints
        if (errMsg.Contains("cannot insert the value null") || errMsg.Contains("mandatory") || errMsg.Contains("required"))
        {
            string field = "Mandatory Form Field";
            if (errMsg.Contains("date") || errMsg.Contains("scheduledate") || errMsg.Contains("expecteddeliverydate"))
                field = "Schedule / Expected Delivery Date";
            else if (errMsg.Contains("warehouse") || errMsg.Contains("warehouseid"))
                field = "Warehouse Location";
            else if (errMsg.Contains("customer") || errMsg.Contains("customerid"))
                field = "Customer";
            else if (errMsg.Contains("supplier") || errMsg.Contains("supplierid"))
                field = "Supplier / Vendor";

            return new RootCauseAnalysisDto
            {
                IssueSummary = $"Missing mandatory value: {field}",
                RootCauseCategory = "ValidationError",
                Detail = $"The database rejected the transaction because '{field}' is required and cannot be left blank.",
                MissingPrerequisite = field,
                RecommendedResolution = $"Please choose or enter a valid {field} before submitting the form.",
                CanAutoFix = false
            };
        }

        // Default General Diagnosis
        return new RootCauseAnalysisDto
        {
            IssueSummary = "System validation or transaction condition not satisfied.",
            RootCauseCategory = "BusinessRuleViolation",
            Detail = !string.IsNullOrEmpty(context.ErrorMessage) ? context.ErrorMessage : "The ERP business rules prevented this operation from completing.",
            RecommendedResolution = "Review all form inputs, verify related record statuses, or check with your system administrator.",
            CanAutoFix = false
        };
    }

    public async Task<AiAnalyzeErrorResponseDto> ExplainErrorAsync(AiAnalyzeErrorRequestDto request, CancellationToken cancellationToken = default)
    {
        var context = request.Context ?? new AiSupportContextDto { ErrorMessage = request.ErrorMessage, ErrorCode = request.ErrorCode, HttpStatus = request.HttpStatus };
        if (string.IsNullOrEmpty(context.ErrorMessage))
        {
            context.ErrorMessage = request.ErrorMessage;
        }

        var diagnosis = await DiagnoseIssueAsync(context, cancellationToken);
        var buttons = new List<AiSuggestedActionBtnDto>();
        AiProposedActionDto? autoFixAction = null;

        string simpleExplanation;
        string whyItHappened;
        string howToFix;

        switch (diagnosis.RootCauseCategory)
        {
            case "StockInsufficient":
                simpleExplanation = "You cannot complete this transaction because the required quantity exceeds available inventory in the selected warehouse.";
                whyItHappened = diagnosis.Detail;
                howToFix = "You can either reduce the requested quantity, select another warehouse that has stock, or raise a Purchase Order to replenish the inventory.";
                buttons.Add(new AiSuggestedActionBtnDto { Label = "View Warehouse Stocks", ActionType = "Navigate", TargetRoute = "/inventory/stock" });
                buttons.Add(new AiSuggestedActionBtnDto { Label = "Smart Purchase Assistant", ActionType = "Navigate", TargetRoute = "/ai-assistant/smart-purchase" });

                if (!string.IsNullOrEmpty(context.SelectedProductId) && Guid.TryParse(context.SelectedProductId, out var pId))
                {
                    var prod = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == pId, cancellationToken);
                    var shortage = context.RequestedQuantity ?? 50m;

                    autoFixAction = new AiProposedActionDto
                    {
                        ActionType = "CreatePurchaseRequest",
                        Title = $"Create Purchase Request for {prod?.Name ?? "Item"}",
                        Description = $"Raise a procurement order for {shortage} {prod?.UnitOfMeasure?.Code ?? "units"} to resolve the warehouse shortage.",
                        RiskLevel = "RequiresConfirmation",
                        Reason = "Stock level is insufficient for customer order fulfillment.",
                        RequiredPermission = Permissions.Purchases.Create,
                        UserHasPermission = _currentUserService.Permissions.Contains(Permissions.Purchases.Create) || _currentUserService.IsSuperAdmin,
                        PreviewTable = new List<AiActionPreviewItemDto>
                        {
                            new() { Field = "Product", Value = prod?.Name ?? "Product" },
                            new() { Field = "SKU", Value = prod?.SKU ?? "SKU" },
                            new() { Field = "Recommended Quantity", Value = $"{shortage} {prod?.UnitOfMeasure?.Code ?? "units"}" },
                            new() { Field = "Destination Warehouse", Value = "Central Warehouse" }
                        },
                        Payload = new Dictionary<string, string>
                        {
                            ["productId"] = pId.ToString(),
                            ["quantity"] = shortage.ToString(),
                            ["notes"] = "AI Automated Reorder for Sales Shortage"
                        }
                    };
                }
                break;

            case "InactiveEntity":
                simpleExplanation = "The selected product or customer is inactive and cannot be used in active transactions.";
                whyItHappened = diagnosis.Detail;
                howToFix = "Reactivate the record in the master catalog or select another active item.";

                if (diagnosis.CanAutoFix && !string.IsNullOrEmpty(context.SelectedProductId))
                {
                    autoFixAction = new AiProposedActionDto
                    {
                        ActionType = "ActivateProduct",
                        Title = "Activate Product in Catalog",
                        Description = "Enable this product so it can be billed on invoices and ordered on POs.",
                        RiskLevel = "RequiresConfirmation",
                        Reason = "Product is currently disabled.",
                        RequiredPermission = Permissions.Inventory.Update,
                        UserHasPermission = true,
                        PreviewTable = new List<AiActionPreviewItemDto>
                        {
                            new() { Field = "Action", Value = "Set IsActive = True" },
                            new() { Field = "Product ID", Value = context.SelectedProductId }
                        },
                        Payload = new Dictionary<string, string>
                        {
                            ["productId"] = context.SelectedProductId
                        }
                    };
                }
                break;

            case "PermissionMissing":
                simpleExplanation = "You do not have the required security role permission to execute this operation.";
                whyItHappened = diagnosis.Detail;
                howToFix = $"Ask your system administrator to assign the '{diagnosis.RequiredPermission}' permission to your role in Settings -> Roles & Security.";
                buttons.Add(new AiSuggestedActionBtnDto { Label = "View Roles & Security", ActionType = "Navigate", TargetRoute = "/settings/roles" });
                break;

            case "ValidationError":
                simpleExplanation = "One or more required fields were missing or invalid when saving the record.";
                whyItHappened = diagnosis.Detail;
                howToFix = diagnosis.RecommendedResolution;
                buttons.Add(new AiSuggestedActionBtnDto { Label = "Review Form Inputs", ActionType = "Retry" });
                break;

            default:
                simpleExplanation = "A business rule or database constraint prevented this operation from completing.";
                whyItHappened = diagnosis.Detail;
                howToFix = diagnosis.RecommendedResolution;
                buttons.Add(new AiSuggestedActionBtnDto { Label = "Try Again", ActionType = "Retry" });
                break;
        }

        return new AiAnalyzeErrorResponseDto
        {
            SimpleExplanation = simpleExplanation,
            WhyItHappened = whyItHappened,
            HowToFix = howToFix,
            RootCause = diagnosis,
            ActionButtons = buttons,
            ProposedAction = autoFixAction
        };
    }
}
