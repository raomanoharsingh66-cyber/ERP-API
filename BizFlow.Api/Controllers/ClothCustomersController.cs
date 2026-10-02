using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.ClothHub;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BizFlow.Api.Controllers;

[Authorize]
[Route("api/clothhub/customers")]
public class ClothCustomersController : BaseApiController
{
    private readonly IClothCustomerService _customerService;

    public ClothCustomersController(IClothCustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ClothCustomerDto>>>> GetCustomers([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var result = await _customerService.GetCustomersAsync(search, cancellationToken);
        return Ok(ApiResponse<List<ClothCustomerDto>>.SuccessResponse(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ClothCustomerDto>>> GetCustomerById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _customerService.GetCustomerByIdAsync(id, cancellationToken);
        if (result == null)
            return NotFound(ApiResponse<ClothCustomerDto>.FailureResponse("Customer account not found"));

        return Ok(ApiResponse<ClothCustomerDto>.SuccessResponse(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ClothCustomerDto>>> CreateCustomer([FromBody] CreateClothCustomerDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _customerService.CreateCustomerAsync(dto, cancellationToken);
            return Ok(ApiResponse<ClothCustomerDto>.SuccessResponse(result, "Customer profile created successfully"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<ClothCustomerDto>.FailureResponse(ex.Message));
        }
    }

    [HttpPost("settle-credit")]
    public async Task<ActionResult<ApiResponse<ClothCustomerDto>>> SettleCredit([FromBody] SettleCustomerCreditDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _customerService.SettleCreditAsync(dto, cancellationToken);
            return Ok(ApiResponse<ClothCustomerDto>.SuccessResponse(result, "Customer credit settlement recorded successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<ClothCustomerDto>.FailureResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<ClothCustomerDto>.FailureResponse(ex.Message));
        }
    }
}
