using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BizFlow.Application.DTOs.AiSupport;

namespace BizFlow.Application.Common.Interfaces;

public interface IAiSupportService
{
    Task<AiSupportChatResponseDto> ChatAsync(AiSupportChatRequestDto request, CancellationToken cancellationToken = default);
    Task<AiAnalyzeErrorResponseDto> AnalyzeErrorAsync(AiAnalyzeErrorRequestDto request, CancellationToken cancellationToken = default);
    Task<WorkflowGuidanceDto> GetNextActionAsync(AiSupportContextDto context, CancellationToken cancellationToken = default);
    Task<AiProposedActionDto?> ProposeActionAsync(AiSupportContextDto context, string actionIntent, CancellationToken cancellationToken = default);
    Task<AiExecuteActionResponseDto> ExecuteActionAsync(AiExecuteActionRequestDto request, CancellationToken cancellationToken = default);
    Task<List<AiKnowledgeItemDto>> GetKnowledgeItemsAsync(string? module = null, string? search = null, CancellationToken cancellationToken = default);
}

public interface IAiKnowledgeService
{
    Task<List<AiKnowledgeItemDto>> SearchKnowledgeAsync(string query, string? module = null, CancellationToken cancellationToken = default);
    Task<AiKnowledgeItemDto?> GetTopicGuideAsync(string topic, CancellationToken cancellationToken = default);
    Task<List<AiKnowledgeItemDto>> GetAllKnowledgeAsync(CancellationToken cancellationToken = default);
}

public interface IAiWorkflowService
{
    Task<WorkflowGuidanceDto> EvaluateWorkflowAsync(AiSupportContextDto context, CancellationToken cancellationToken = default);
}

public interface IAiTroubleshootingService
{
    Task<RootCauseAnalysisDto> DiagnoseIssueAsync(AiSupportContextDto context, CancellationToken cancellationToken = default);
    Task<AiAnalyzeErrorResponseDto> ExplainErrorAsync(AiAnalyzeErrorRequestDto request, CancellationToken cancellationToken = default);
}

public interface IAiActionService
{
    Task<AiProposedActionDto?> PlanActionAsync(AiSupportContextDto context, string intent, CancellationToken cancellationToken = default);
    Task<AiExecuteActionResponseDto> ExecuteConfirmedActionAsync(AiExecuteActionRequestDto request, CancellationToken cancellationToken = default);
}

public interface IAiAuditService
{
    Task<long> LogAiActionAsync(
        Guid userId,
        Guid businessId,
        string sessionId,
        string userRequest,
        string actionType,
        string payloadJson,
        bool userConfirmed,
        bool success,
        string resultMessage,
        string? recordId = null,
        CancellationToken cancellationToken = default);
}
