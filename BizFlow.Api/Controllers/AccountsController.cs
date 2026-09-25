using BizFlow.Api.Attributes;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Accounting;
using BizFlow.Domain.Constants;
using BizFlow.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace BizFlow.Api.Controllers;

public class AccountsController : BaseApiController
{
    private readonly IAccountingService _accountingService;

    public AccountsController(IAccountingService accountingService)
    {
        _accountingService = accountingService;
    }

    [HttpGet]
    [HasPermission(Permissions.Accounting.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AccountDto>>>> GetAccounts(
        [FromQuery] AccountType? type,
        CancellationToken cancellationToken)
    {
        var accounts = await _accountingService.GetAccountsAsync(type, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<AccountDto>>.SuccessResponse(accounts));
    }

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Accounting.View)]
    public async Task<ActionResult<ApiResponse<AccountDto>>> GetAccountById(Guid id, CancellationToken cancellationToken)
    {
        var account = await _accountingService.GetAccountByIdAsync(id, cancellationToken);
        if (account == null)
        {
            return NotFound(ApiResponse<AccountDto>.FailureResponse("Account not found."));
        }
        return Ok(ApiResponse<AccountDto>.SuccessResponse(account));
    }

    [HttpPost]
    [HasPermission(Permissions.Accounting.CreateEntry)]
    public async Task<ActionResult<ApiResponse<AccountDto>>> CreateAccount(
        [FromBody] CreateAccountDto dto,
        CancellationToken cancellationToken)
    {
        var account = await _accountingService.CreateAccountAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetAccountById), new { id = account.Id }, ApiResponse<AccountDto>.SuccessResponse(account, "Account created successfully."));
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Accounting.CreateEntry)]
    public async Task<ActionResult<ApiResponse<AccountDto>>> UpdateAccount(
        Guid id,
        [FromBody] UpdateAccountDto dto,
        CancellationToken cancellationToken)
    {
        var account = await _accountingService.UpdateAccountAsync(id, dto, cancellationToken);
        return Ok(ApiResponse<AccountDto>.SuccessResponse(account, "Account updated successfully."));
    }
}
