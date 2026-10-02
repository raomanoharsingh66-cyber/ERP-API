using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.DTOs.AiSupport;

namespace BizFlow.Infrastructure.Services.AiSupport;

public class AiKnowledgeService : IAiKnowledgeService
{
    private static readonly List<AiKnowledgeItemDto> KnowledgeBase = new()
    {
        // 1. Sales & Invoicing
        new AiKnowledgeItemDto
        {
            Module = "Sales",
            Topic = "Create Sales Invoice",
            Question = "How do I create and issue a Sales Invoice?",
            Answer = "To generate a GST-compliant sales invoice, select an active customer, choose the source warehouse with sufficient stock, add product line items with quantities, verify CGST/SGST/IGST tax rates, and click Save.",
            Workflow = "Customer Order -> Sales Order (Optional) -> Sales Invoice -> Stock Deduction -> Payment Receipt",
            StepByStepGuide = new List<string>
            {
                "Navigate to Sales -> Sales Invoices (or click '+ Create Invoice').",
                "Select the billing Customer from the dropdown.",
                "Select the Fulfillment Warehouse holding the physical stock.",
                "Set the Invoice Date and Payment Due Date.",
                "Add Line Items: choose Product SKU, specify Quantity, and verify Unit Price and GST Tax %.",
                "Review the Subtotal, Tax Breakdown (CGST+SGST or IGST), and Grand Total.",
                "Click 'Create Sales Invoice' to issue the document and debit warehouse stock."
            },
            CommonErrors = new List<string>
            {
                "Insufficient stock in warehouse: Cannot dispatch more items than physically available.",
                "Customer missing GSTIN/State: Required for interstate IGST determination.",
                "Product inactive: Inactive SKUs cannot be billed until activated."
            }
        },

        // 2. Procurement & Purchase Orders
        new AiKnowledgeItemDto
        {
            Module = "Purchases",
            Topic = "Purchase Order Workflow",
            Question = "What is the complete purchase order workflow?",
            Answer = "Procurement in BizFlow ERP follows: Purchase Order (Draft) -> Manager Approval -> Goods Receipt Note (GRN) upon physical arrival -> Vendor Bill generation -> Vendor Payment voucher.",
            Workflow = "Purchase Request/Reorder -> Purchase Order (Draft) -> PO Approved -> Physical Delivery -> Goods Receipt Note (GRN) -> Vendor Bill -> Payment",
            StepByStepGuide = new List<string>
            {
                "Create Purchase Order: Go to Purchases -> Purchase Orders -> 'Create Purchase Order'.",
                "Choose Supplier/Vendor, Destination Warehouse, and Expected Delivery Date.",
                "Add Products, specify Ordered Quantity, and agreed Purchase Price.",
                "Manager Approval: Users with 'Purchases.Approve' permission click 'Approve' on the PO.",
                "Inward Goods (GRN): When truck arrives, click 'Receive GRN', enter Delivery Challan #, and record accepted/rejected quantities.",
                "Vendor Bill: Go to Purchases -> Vendor Bills -> create bill matching GRN for Accounts Payable ledger."
            },
            CommonErrors = new List<string>
            {
                "Missing approval permission: Only users with Purchases.Approve can approve POs.",
                "Cannot create GRN: PO must be in Approved or Partially Received status before receiving goods.",
                "Vendor GSTIN mismatch: Check supplier master profile for valid tax registration."
            }
        },

        // 3. Goods Receipt Notes (GRN) & Inward
        new AiKnowledgeItemDto
        {
            Module = "Purchases",
            Topic = "Goods Receipt Note (GRN)",
            Question = "What is a GRN and when should I create it?",
            Answer = "A Goods Receipt Note (GRN) records the physical arrival and quality inspection of merchandise at your warehouse against an approved Purchase Order. It automatically updates inventory stock on hand and recalculates Weighted Average Cost (WAC).",
            Workflow = "PO Approved -> Goods Arrival -> Store Keeper Inspection -> GRN Creation -> Physical Stock Credited",
            StepByStepGuide = new List<string>
            {
                "Locate the Approved Purchase Order in Purchases -> Purchase Orders.",
                "Click the green 'Receive GRN' button.",
                "Enter the Vendor Delivery Challan / Lorry Receipt (LR) number.",
                "Specify Received Quantity, Accepted Quantity (credited to stock), and Rejected Quantity.",
                "If items are rejected, enter the Rejection Reason for vendor quality tracking.",
                "Submit GRN: Physical stock is immediately credited to the designated warehouse."
            },
            CommonErrors = new List<string>
            {
                "Over-receiving goods: GRN accepted quantity cannot exceed remaining PO ordered balance.",
                "PO not approved: Cannot create GRN for Draft POs."
            }
        },

        // 4. Inventory & Stock Management
        new AiKnowledgeItemDto
        {
            Module = "Inventory",
            Topic = "Stock Shortages & Reorders",
            Question = "Why is a product showing as Low Stock or Critical?",
            Answer = "BizFlow ERP continuously calculates stock coverage: Available Stock = Quantity on Hand - Quantity Reserved. If available stock drops below the MinStockLevel (Reorder Level) or expected consumption during vendor lead time exceeds available inventory, the item is flagged as Low or Critical.",
            Workflow = "Physical Consumption -> Available Stock < Reorder Level -> Low Stock Alert -> Smart Purchase Reorder -> PO Created",
            StepByStepGuide = new List<string>
            {
                "Check AI Assistant -> Smart Purchase Assistant to view Reorder Recommendations.",
                "Inspect the 'Why this recommendation?' breakdown for lead-time demand calculations.",
                "Click 'Create Purchase Request' to generate a pre-filled PO with the recommended quantity.",
                "Alternatively, perform a Stock Transfer if another branch or warehouse has surplus stock."
            },
            CommonErrors = new List<string>
            {
                "Physical stock hand count differs from system: Perform an Inventory Stock Adjustment (Inventory -> Stock Management -> Adjust Stock).",
                "Product inactive: Inactive products will not trigger automatic purchase warnings."
            }
        },

        // 5. Product Catalog & Master Data
        new AiKnowledgeItemDto
        {
            Module = "Inventory",
            Topic = "Product Master & Activation",
            Question = "Why is my product not showing in sales invoice or purchase order dropdowns?",
            Answer = "Products only appear in dropdowns when they are marked as 'Active' (`IsActive = true`) and belong to your authorized Business/Tenant context. If a product was deactivated or archived, it must be reactivated.",
            Workflow = "Product Catalog -> SKU Creation -> Category & UOM Assignment -> Active Status -> Available for Transactions",
            StepByStepGuide = new List<string>
            {
                "Open Inventory -> Product Catalog.",
                "Search for the product name or SKU.",
                "Check the 'Status' column. If 'Inactive', click 'Edit'.",
                "Toggle 'Active Status' to checked and click Save.",
                "BizFlow AI can also auto-activate inactive products upon your confirmation."
            },
            CommonErrors = new List<string>
            {
                "Missing Category or Unit of Measure: Both are mandatory for inventory classification.",
                "Duplicate SKU code: Each SKU in a tenant must be unique."
            }
        },

        // 6. Security & Roles
        new AiKnowledgeItemDto
        {
            Module = "Settings",
            Topic = "Permissions & Approval Hierarchy",
            Question = "Why am I getting a 403 Forbidden or missing permission error?",
            Answer = "BizFlow ERP enforces granular Role-Based Access Control (RBAC). Actions like approving purchase orders (`Purchases.Approve`), adjusting stock (`Inventory.AdjustStock`), or managing users (`Users.Create`) require explicit permission assignments on your Role.",
            Workflow = "User Profile -> Assigned Role -> Role Permissions -> Allowed Operations",
            StepByStepGuide = new List<string>
            {
                "Identify the required permission mentioned by BizFlow AI.",
                "Contact your Business Administrator or System Admin.",
                "Admin navigates to Settings -> Roles & Security -> Edit Role.",
                "Check the required permission checkbox and save role changes.",
                "Log out and log back in to refresh JWT claim tokens."
            },
            CommonErrors = new List<string>
            {
                "Token expired: Log out and log back in if permissions were updated recently.",
                "Role not assigned: Verify user account has an active role in Settings -> Staff & Users."
            }
        }
    };

    public Task<List<AiKnowledgeItemDto>> SearchKnowledgeAsync(string query, string? module = null, CancellationToken cancellationToken = default)
    {
        var q = query.ToLower().Trim();
        var results = KnowledgeBase.Where(k =>
            (string.IsNullOrEmpty(module) || k.Module.Equals(module, StringComparison.OrdinalIgnoreCase)) &&
            (k.Topic.ToLower().Contains(q) ||
             k.Question.ToLower().Contains(q) ||
             k.Answer.ToLower().Contains(q) ||
             (k.Workflow != null && k.Workflow.ToLower().Contains(q)) ||
             k.CommonErrors.Any(e => e.ToLower().Contains(q)) ||
             k.StepByStepGuide.Any(s => s.ToLower().Contains(q)))
        ).ToList();

        if (!results.Any() && !string.IsNullOrEmpty(module))
        {
            results = KnowledgeBase.Where(k => k.Module.Equals(module, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        return Task.FromResult(results);
    }

    public Task<AiKnowledgeItemDto?> GetTopicGuideAsync(string topic, CancellationToken cancellationToken = default)
    {
        var item = KnowledgeBase.FirstOrDefault(k => k.Topic.Equals(topic, StringComparison.OrdinalIgnoreCase) ||
                                                    k.Topic.ToLower().Contains(topic.ToLower()));
        return Task.FromResult(item);
    }

    public Task<List<AiKnowledgeItemDto>> GetAllKnowledgeAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(KnowledgeBase.ToList());
    }
}
