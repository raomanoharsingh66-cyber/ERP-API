using System;
using System.Collections.Generic;

namespace BizFlow.Application.DTOs.AiSupport;

public class AiSupportContextDto
{
    public string Module { get; set; } = string.Empty; // e.g., "Sales", "Inventory", "Purchases", "Accounting", "Settings"
    public string Page { get; set; } = string.Empty; // e.g., "Create Invoice", "Products List", "Purchase Orders"
    public string Route { get; set; } = string.Empty; // e.g., "/sales/create", "/inventory/products"
    public string? RecordId { get; set; } // Current transaction ID if applicable
    public string? RecordType { get; set; } // "SalesInvoice", "PurchaseOrder", "Product", etc.
    public string? WorkflowStep { get; set; } // "Draft", "Approved", "Received", "StockChecking"
    public string? Field { get; set; } // Active field if triggered from specific input
    public string? SelectedProductId { get; set; }
    public string? SelectedCustomerId { get; set; }
    public string? SelectedSupplierId { get; set; }
    public string? SelectedWarehouseId { get; set; }
    public decimal? RequestedQuantity { get; set; }
    public Dictionary<string, string>? FormState { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }
    public int? HttpStatus { get; set; }
}

public class AiChatMessageDto
{
    public string Role { get; set; } = "user"; // "user", "assistant", "system"
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public AiProposedActionDto? ProposedAction { get; set; }
}

public class AiSupportChatRequestDto
{
    public string SessionId { get; set; } = Guid.NewGuid().ToString();
    public string Message { get; set; } = string.Empty;
    public AiSupportContextDto? Context { get; set; }
    public List<AiChatMessageDto> History { get; set; } = new();
}

public class AiSupportChatResponseDto
{
    public string SessionId { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public List<string> SuggestedPrompts { get; set; } = new();
    public AiProposedActionDto? ProposedAction { get; set; }
    public WorkflowGuidanceDto? WorkflowGuidance { get; set; }
    public RootCauseAnalysisDto? RootCause { get; set; }
    public List<string>? StepByStepGuide { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class AiAnalyzeErrorRequestDto
{
    public string ErrorMessage { get; set; } = string.Empty;
    public string? ErrorCode { get; set; }
    public int? HttpStatus { get; set; }
    public AiSupportContextDto? Context { get; set; }
}

public class AiAnalyzeErrorResponseDto
{
    public string SimpleExplanation { get; set; } = string.Empty;
    public string WhyItHappened { get; set; } = string.Empty;
    public string HowToFix { get; set; } = string.Empty;
    public RootCauseAnalysisDto? RootCause { get; set; }
    public List<AiSuggestedActionBtnDto> ActionButtons { get; set; } = new();
    public AiProposedActionDto? ProposedAction { get; set; }
}

public class AiSuggestedActionBtnDto
{
    public string Label { get; set; } = string.Empty;
    public string ActionType { get; set; } = "Navigate"; // "Navigate", "ProposeFix", "Retry"
    public string? TargetRoute { get; set; }
    public Dictionary<string, string>? Params { get; set; }
}

public class WorkflowGuidanceDto
{
    public string Module { get; set; } = string.Empty;
    public string CurrentStage { get; set; } = string.Empty;
    public string CurrentStageDescription { get; set; } = string.Empty;
    public List<string> CompletedStages { get; set; } = new();
    public string? NextStage { get; set; }
    public string? NextStageDescription { get; set; }
    public List<WorkflowActionOptionDto> AllowedNextActions { get; set; } = new();
    public bool IsBlocked { get; set; }
    public string? BlockReason { get; set; }
    public List<string> Prerequisites { get; set; } = new();
}

public class WorkflowActionOptionDto
{
    public string Title { get; set; } = string.Empty;
    public string ActionCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? TargetRoute { get; set; }
    public string RequiredPermission { get; set; } = string.Empty;
    public bool CanExecute { get; set; } = true;
}

public class RootCauseAnalysisDto
{
    public string IssueSummary { get; set; } = string.Empty;
    public string RootCauseCategory { get; set; } = "General"; // "PermissionMissing", "StockInsufficient", "InactiveEntity", "ValidationError", "WorkflowPrerequisiteMissing", "BusinessRuleViolation"
    public string Detail { get; set; } = string.Empty;
    public string? RequiredPermission { get; set; }
    public string? MissingPrerequisite { get; set; }
    public string RecommendedResolution { get; set; } = string.Empty;
    public bool CanAutoFix { get; set; }
}

public class AiActionPreviewItemDto
{
    public string Field { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public class AiProposedActionDto
{
    public Guid ActionId { get; set; } = Guid.NewGuid();
    public string ActionType { get; set; } = string.Empty; // "CreatePurchaseRequest", "ActivateProduct", "Navigate", "CreateSalesInvoiceDraft", "AdjustStock"
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string RiskLevel { get; set; } = "RequiresConfirmation"; // "Low", "RequiresConfirmation", "High"
    public string Reason { get; set; } = string.Empty;
    public List<AiActionPreviewItemDto> PreviewTable { get; set; } = new();
    public Dictionary<string, string> Payload { get; set; } = new();
    public string RequiredPermission { get; set; } = string.Empty;
    public bool UserHasPermission { get; set; } = true;
}

public class AiExecuteActionRequestDto
{
    public string SessionId { get; set; } = string.Empty;
    public Guid ActionId { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public Dictionary<string, string> Payload { get; set; } = new();
    public bool UserConfirmed { get; set; } = true;
}

public class AiExecuteActionResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public string? RedirectRoute { get; set; }
    public string? RecordId { get; set; }
    public long AuditLogId { get; set; }
    public string? FailedDetails { get; set; }
}

public class AiKnowledgeItemDto
{
    public string Module { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public string? Workflow { get; set; }
    public List<string> StepByStepGuide { get; set; } = new();
    public List<string> CommonErrors { get; set; } = new();
}
