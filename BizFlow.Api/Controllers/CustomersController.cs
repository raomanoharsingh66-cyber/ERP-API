using BizFlow.Api.Attributes;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Sales;
using BizFlow.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace BizFlow.Api.Controllers;

public class CustomersController : BaseApiController
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpGet]
    [HasPermission(Permissions.Customers.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<CustomerDto>>>> GetCustomers(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _customerService.GetCustomersAsync(search, isActive, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Customers.View)]
    public async Task<ActionResult<ApiResponse<CustomerDto>>> GetCustomerById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _customerService.GetCustomerByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [HasPermission(Permissions.Customers.Create)]
    public async Task<ActionResult<ApiResponse<CustomerDto>>> CreateCustomer(
        [FromBody] CreateCustomerDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _customerService.CreateCustomerAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetCustomerById), new { id = result.Data!.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Customers.Update)]
    public async Task<ActionResult<ApiResponse<CustomerDto>>> UpdateCustomer(
        Guid id,
        [FromBody] UpdateCustomerDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _customerService.UpdateCustomerAsync(id, dto, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.Customers.Delete)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteCustomer(Guid id, CancellationToken cancellationToken)
    {
        var result = await _customerService.DeleteCustomerAsync(id, cancellationToken);
        return Ok(result);
    }
}
