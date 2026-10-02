using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.AiSupport;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BizFlow.Api.Controllers;

[Authorize]
[Route("api/ai/support")]
[Route("api/ai-support")]
public class AiSupportController : BaseApiController
{
    private readonly IAiSupportService _aiSupportService;
    private readonly ICurrentUserService _currentUserService;

    public AiSupportController(IAiSupportService aiSupportService, ICurrentUserService currentUserService)
    {
        _aiSupportService = aiSupportService;
        _currentUserService = currentUserService;
    }

    [HttpPost("chat")]
    public async Task<ActionResult<ApiResponse<AiSupportChatResponseDto>>> Chat(
        [FromBody] AiSupportChatRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _aiSupportService.ChatAsync(request, cancellationToken);
        return Ok(ApiResponse<AiSupportChatResponseDto>.SuccessResponse(result));
    }

    [HttpPost("analyze-error")]
    public async Task<ActionResult<ApiResponse<AiAnalyzeErrorResponseDto>>> AnalyzeError(
        [FromBody] AiAnalyzeErrorRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _aiSupportService.AnalyzeErrorAsync(request, cancellationToken);
        return Ok(ApiResponse<AiAnalyzeErrorResponseDto>.SuccessResponse(result));
    }

    [HttpGet("context")]
    public ActionResult<ApiResponse<object>> GetCurrentContext([FromQuery] string? route, [FromQuery] string? page)
    {
        var context = new
        {
            UserId = _currentUserService.UserId,
            UserEmail = _currentUserService.Email,
            BusinessId = _currentUserService.BusinessId,
            Roles = _currentUserService.Roles,
            Permissions = _currentUserService.Permissions,
            CurrentRoute = route ?? "/",
            CurrentPage = page ?? "Dashboard",
            Timestamp = DateTime.UtcNow
        };

        return Ok(ApiResponse<object>.SuccessResponse(context));
    }

    [HttpPost("next-action")]
    public async Task<ActionResult<ApiResponse<WorkflowGuidanceDto>>> GetNextAction(
        [FromBody] AiSupportContextDto context,
        CancellationToken cancellationToken)
    {
        var result = await _aiSupportService.GetNextActionAsync(context, cancellationToken);
        return Ok(ApiResponse<WorkflowGuidanceDto>.SuccessResponse(result));
    }

    [HttpPost("propose-action")]
    public async Task<ActionResult<ApiResponse<AiProposedActionDto?>>> ProposeAction(
        [FromBody] ProposeActionRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _aiSupportService.ProposeActionAsync(request.Context, request.Intent, cancellationToken);
        return Ok(ApiResponse<AiProposedActionDto?>.SuccessResponse(result));
    }

    [HttpPost("execute-action")]
    public async Task<ActionResult<ApiResponse<AiExecuteActionResponseDto>>> ExecuteAction(
        [FromBody] AiExecuteActionRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _aiSupportService.ExecuteActionAsync(request, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(ApiResponse<AiExecuteActionResponseDto>.FailureResponse(result.Message, result.FailedDetails ?? result.Message));
        }

        return Ok(ApiResponse<AiExecuteActionResponseDto>.SuccessResponse(result));
    }

    [HttpGet("workflow/{module}")]
    [HttpGet("workflow/{module}/{recordId}")]
    public async Task<ActionResult<ApiResponse<WorkflowGuidanceDto>>> GetWorkflow(
        string module,
        string? recordId,
        CancellationToken cancellationToken)
    {
        var context = new AiSupportContextDto
        {
            Module = module,
            RecordId = recordId,
            Route = $"/{module.ToLower()}"
        };

        var result = await _aiSupportService.GetNextActionAsync(context, cancellationToken);
        return Ok(ApiResponse<WorkflowGuidanceDto>.SuccessResponse(result));
    }

    [HttpGet("knowledge")]
    public async Task<ActionResult<ApiResponse<List<AiKnowledgeItemDto>>>> GetKnowledge(
        [FromQuery] string? module,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var result = await _aiSupportService.GetKnowledgeItemsAsync(module, search, cancellationToken);
        return Ok(ApiResponse<List<AiKnowledgeItemDto>>.SuccessResponse(result));
    }
}

public class ProposeActionRequestDto
{
    public AiSupportContextDto Context { get; set; } = new();
    public string Intent { get; set; } = string.Empty;
}
