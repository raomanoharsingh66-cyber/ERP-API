using BizFlow.Api.Attributes;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Accounting;
using BizFlow.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace BizFlow.Api.Controllers;

[Route("api/[controller]")]
[Route("api/journal-entries")]
public class JournalEntriesController : BaseApiController
{
    private readonly IAccountingService _accountingService;

    public JournalEntriesController(IAccountingService accountingService)
    {
        _accountingService = accountingService;
    }

    [HttpGet]
    [HasPermission(Permissions.Accounting.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<JournalEntryDto>>>> GetJournalEntries(
        [FromQuery] JournalEntryFilterDto filter,
        CancellationToken cancellationToken)
    {
        var result = await _accountingService.GetJournalEntriesAsync(filter, cancellationToken);
        return Ok(ApiResponse<PagedResult<JournalEntryDto>>.SuccessResponse(result));
    }

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Accounting.View)]
    public async Task<ActionResult<ApiResponse<JournalEntryDto>>> GetJournalEntryById(Guid id, CancellationToken cancellationToken)
    {
        var entry = await _accountingService.GetJournalEntryByIdAsync(id, cancellationToken);
        if (entry == null)
        {
            return NotFound(ApiResponse<JournalEntryDto>.FailureResponse("Journal voucher not found."));
        }
        return Ok(ApiResponse<JournalEntryDto>.SuccessResponse(entry));
    }

    [HttpPost]
    [HasPermission(Permissions.Accounting.CreateEntry)]
    public async Task<ActionResult<ApiResponse<JournalEntryDto>>> CreateJournalEntry(
        [FromBody] CreateJournalEntryDto dto,
        CancellationToken cancellationToken)
    {
        var entry = await _accountingService.CreateJournalEntryAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetJournalEntryById), new { id = entry.Id }, ApiResponse<JournalEntryDto>.SuccessResponse(entry, "Journal entry posted successfully."));
    }
}
