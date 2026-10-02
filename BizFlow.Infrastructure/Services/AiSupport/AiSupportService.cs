using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.DTOs.AiSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BizFlow.Infrastructure.Services.AiSupport;

public class AiSupportService : IAiSupportService
{
    private readonly IAiKnowledgeService _knowledgeService;
    private readonly IAiWorkflowService _workflowService;
    private readonly IAiTroubleshootingService _troubleshootingService;
    private readonly IAiActionService _actionService;
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<AiSupportService> _logger;

    public AiSupportService(
        IAiKnowledgeService knowledgeService,
        IAiWorkflowService workflowService,
        IAiTroubleshootingService troubleshootingService,
        IAiActionService actionService,
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ILogger<AiSupportService> logger)
    {
        _knowledgeService = knowledgeService;
        _workflowService = workflowService;
        _troubleshootingService = troubleshootingService;
        _actionService = actionService;
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<AiSupportChatResponseDto> ChatAsync(AiSupportChatRequestDto request, CancellationToken cancellationToken = default)
    {
        var msg = (request.Message ?? string.Empty).Trim().ToLower();
        var context = request.Context ?? new AiSupportContextDto();
        var history = request.History ?? new List<AiChatMessageDto>();

        // Conversation memory extraction: check if past messages referred to a specific product or error
        var lastUserMsg = history.LastOrDefault(h => h.Role == "user")?.Content.ToLower() ?? string.Empty;
        var lastAssistantMsg = history.LastOrDefault(h => h.Role == "assistant")?.Content.ToLower() ?? string.Empty;

        string answer;
        var suggestedPrompts = new List<string>();
        AiProposedActionDto? proposedAction = null;
        WorkflowGuidanceDto? workflowGuidance = null;
        RootCauseAnalysisDto? rootCause = null;
        List<string>? steps = null;

        // 1. "What should I do next?" / "I am stuck"
        if (msg.Contains("what should i do next") || msg.Contains("stuck") || msg.Contains("what next") || msg.Contains("next step"))
        {
            workflowGuidance = await _workflowService.EvaluateWorkflowAsync(context, cancellationToken);
            answer = $"### ⚡ Workflow Guidance: {workflowGuidance.CurrentStage}\n\n" +
                     $"{workflowGuidance.CurrentStageDescription}\n\n" +
                     $"**Next Step:** {workflowGuidance.NextStage}\n" +
                     $"{workflowGuidance.NextStageDescription}\n\n";

            if (workflowGuidance.IsBlocked)
            {
                answer += $"> ⚠️ **Current Action Blocked:** {workflowGuidance.BlockReason}\n\n";
            }

            if (workflowGuidance.AllowedNextActions.Any())
            {
                answer += "#### Recommended Next Actions:\n";
                foreach (var action in workflowGuidance.AllowedNextActions)
                {
                    answer += $"* **{action.Title}**: {action.Description}\n";
                }
            }

            suggestedPrompts.Add("Why is this blocked?");
            suggestedPrompts.Add("How do I complete this workflow?");
            suggestedPrompts.Add("Can you fix this for me?");
        }
        // 2. "Why can't I create this invoice?" / "Why is this blocked?" / Stock Shortage
        else if (msg.Contains("why can't i create") || msg.Contains("why is this blocked") || msg.Contains("cannot create") || msg.Contains("why blocked") || msg.Contains("stock is insufficient"))
        {
            rootCause = await _troubleshootingService.DiagnoseIssueAsync(context, cancellationToken);
            answer = $"### 🔍 Root-Cause Diagnosis: {rootCause.IssueSummary}\n\n" +
                     $"**Why this happened:**\n{rootCause.Detail}\n\n" +
                     $"**Recommended Resolution:**\n{rootCause.RecommendedResolution}\n\n";

            if (rootCause.RootCauseCategory == "StockInsufficient")
            {
                answer += "> 💡 *Tip: BizFlow AI can prepare an automated Purchase Order to replenish this shortage.*";
                proposedAction = await _actionService.PlanActionAsync(context, "purchase", cancellationToken);
            }
            else if (rootCause.RootCauseCategory == "InactiveEntity" && rootCause.CanAutoFix)
            {
                answer += "> 💡 *Tip: BizFlow AI can reactivate this item for you right now.*";
                proposedAction = await _actionService.PlanActionAsync(context, "activate", cancellationToken);
            }

            suggestedPrompts.Add("Can you create a purchase request?");
            suggestedPrompts.Add("Check other warehouses");
            suggestedPrompts.Add("What should I do next?");
        }
        // 3. "Explain this error" / "Why did this error happen?"
        else if (msg.Contains("error") || msg.Contains("why did this happen") || msg.Contains("how can i fix this"))
        {
            var errAnalysis = await _troubleshootingService.ExplainErrorAsync(new AiAnalyzeErrorRequestDto
            {
                ErrorMessage = context.ErrorMessage ?? "Transaction validation constraint failed.",
                Context = context
            }, cancellationToken);

            rootCause = errAnalysis.RootCause;
            proposedAction = errAnalysis.ProposedAction;

            answer = $"### 🛠 Error Breakdown\n\n" +
                     $"**Summary:** {errAnalysis.SimpleExplanation}\n\n" +
                     $"**Root Cause:** {errAnalysis.WhyItHappened}\n\n" +
                     $"**How to Fix:** {errAnalysis.HowToFix}\n\n";

            suggestedPrompts.Add("What should I do next?");
            suggestedPrompts.Add("Can you fix this for me?");
        }
        // 4. "Can you fix this for me?" / "Create a purchase request" / "Activate product"
        else if (msg.Contains("fix this for me") || msg.Contains("can you fix") || msg.Contains("create a purchase request") || msg.Contains("create purchase") || msg.Contains("activate"))
        {
            proposedAction = await _actionService.PlanActionAsync(context, msg, cancellationToken);
            if (proposedAction != null)
            {
                answer = $"### 🤖 Action Prepared: {proposedAction.Title}\n\n" +
                         $"{proposedAction.Description}\n\n" +
                         $"**Reason:** {proposedAction.Reason}\n\n" +
                         $"*Please inspect the action preview table below. When ready, click **Confirm & Execute**.*";
            }
            else
            {
                answer = "I checked the current context, but no automatic corrective action is required or permissible right now. You can ask: *'What should I do next?'* or check the step-by-step guide.";
            }

            suggestedPrompts.Add("What should I do next?");
            suggestedPrompts.Add("Show workflow guide");
        }
        // 5. "Which product is causing the issue?" / "How much stock do I need?" (Contextual conversational memory)
        else if (msg.Contains("which product") || msg.Contains("how much stock"))
        {
            if (!string.IsNullOrEmpty(context.SelectedProductId) && Guid.TryParse(context.SelectedProductId, out var memProdId))
            {
                var prod = await _context.Products.AsNoTracking().Include(p => p.UnitOfMeasure).FirstOrDefaultAsync(p => p.Id == memProdId, cancellationToken);
                var stocks = await _context.InventoryStocks.AsNoTracking().Where(s => s.ProductId == memProdId).ToListAsync(cancellationToken);
                var totalAvail = stocks.Sum(s => s.QuantityOnHand - s.QuantityReserved);
                var req = context.RequestedQuantity ?? 1m;
                var shortage = Math.Max(0m, req - totalAvail);

                answer = $"### Product In Question: {prod?.Name ?? "Item"}\n\n" +
                         $"* **SKU**: `{prod?.SKU ?? "N/A"}`\n" +
                         $"* **Required Quantity**: {req} {prod?.UnitOfMeasure?.Code ?? "units"}\n" +
                         $"* **Currently Available**: {totalAvail} {prod?.UnitOfMeasure?.Code ?? "units"}\n" +
                         $"* **Shortage Needed**: **{shortage} {prod?.UnitOfMeasure?.Code ?? "units"}**\n\n" +
                         $"Would you like me to prepare a purchase request for this quantity?";

                proposedAction = await _actionService.PlanActionAsync(context, "purchase", cancellationToken);
            }
            else
            {
                answer = "Based on your active screen, please select a product line item in the form so I can evaluate exact warehouse stock and shortage numbers.";
            }

            suggestedPrompts.Add("Can you create a purchase request?");
            suggestedPrompts.Add("Check other warehouses");
        }
        // 6. "How do I create a sales invoice?" / "How do I..." (Knowledge Base)
        else if (msg.Contains("how do i") || msg.Contains("how to create") || msg.Contains("guide") || msg.Contains("help"))
        {
            var searchResults = await _knowledgeService.SearchKnowledgeAsync(msg, context.Module, cancellationToken);
            var item = searchResults.FirstOrDefault();

            if (item != null)
            {
                steps = item.StepByStepGuide;
                answer = $"### 📖 Step-by-Step Guide: {item.Topic}\n\n" +
                         $"{item.Answer}\n\n";

                if (item.StepByStepGuide.Any())
                {
                    answer += "#### Instructions:\n";
                    for (int i = 0; i < item.StepByStepGuide.Count; i++)
                    {
                        answer += $"{i + 1}. {item.StepByStepGuide[i]}\n";
                    }
                }
            }
            else
            {
                answer = "I can guide you step-by-step through any BizFlow ERP workflow (Sales Invoices, Purchase Orders, GRNs, Inventory Counts, or Vendor Bills). What would you like to achieve?";
            }

            suggestedPrompts.Add("How to create a Sales Invoice?");
            suggestedPrompts.Add("How to receive a GRN?");
            suggestedPrompts.Add("What should I do next?");
        }
        // 7. General Contextual Copilot Response
        else
        {
            var page = !string.IsNullOrEmpty(context.Page) ? context.Page : "BizFlow ERP";
            var mod = !string.IsNullOrEmpty(context.Module) ? context.Module : "General";

            answer = $"### 🤖 BizFlow AI Copilot\n\n" +
                     $"I am tracking your current position in **{mod} &rarr; {page}**.\n\n" +
                     $"Here is what I can do for you right now:\n" +
                     $"- Tell you **'What should I do next?'** in the current transaction\n" +
                     $"- Diagnose and explain why an invoice, PO, or button is blocked\n" +
                     $"- Provide plain-language root-cause explanations for any system error\n" +
                     $"- Safely draft purchase orders or activate products upon your confirmation\n" +
                     $"- Guide you through workflows step-by-step.";

            suggestedPrompts.Add("What should I do next?");
            suggestedPrompts.Add("Why is this blocked?");
            suggestedPrompts.Add("How do I create a sales invoice?");
        }

        return new AiSupportChatResponseDto
        {
            SessionId = request.SessionId,
            Answer = answer,
            SuggestedPrompts = suggestedPrompts,
            ProposedAction = proposedAction,
            WorkflowGuidance = workflowGuidance,
            RootCause = rootCause,
            StepByStepGuide = steps,
            Timestamp = DateTime.UtcNow
        };
    }

    public Task<AiAnalyzeErrorResponseDto> AnalyzeErrorAsync(AiAnalyzeErrorRequestDto request, CancellationToken cancellationToken = default)
    {
        return _troubleshootingService.ExplainErrorAsync(request, cancellationToken);
    }

    public Task<WorkflowGuidanceDto> GetNextActionAsync(AiSupportContextDto context, CancellationToken cancellationToken = default)
    {
        return _workflowService.EvaluateWorkflowAsync(context, cancellationToken);
    }

    public Task<AiProposedActionDto?> ProposeActionAsync(AiSupportContextDto context, string actionIntent, CancellationToken cancellationToken = default)
    {
        return _actionService.PlanActionAsync(context, actionIntent, cancellationToken);
    }

    public Task<AiExecuteActionResponseDto> ExecuteActionAsync(AiExecuteActionRequestDto request, CancellationToken cancellationToken = default)
    {
        return _actionService.ExecuteConfirmedActionAsync(request, cancellationToken);
    }

    public Task<List<AiKnowledgeItemDto>> GetKnowledgeItemsAsync(string? module = null, string? search = null, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrEmpty(search))
        {
            return _knowledgeService.SearchKnowledgeAsync(search, module, cancellationToken);
        }

        return _knowledgeService.GetAllKnowledgeAsync(cancellationToken);
    }
}
