using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesForecastSystem.Core.Common;
using SalesForecastSystem.Core.DTOs.Customers;
using SalesForecastSystem.Core.Helpers;
using SalesForecastSystem.Core.Interfaces.Services;

namespace SalesForecastSystem.API.Controllers;

[ApiController]
[Route("api/customers")]
[Authorize(Roles = RoleNames.Admin + "," + RoleNames.SalesStaff)]
public sealed class CustomerController(ICustomerService service) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<CustomerResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPagedAsync([FromQuery] CustomerQueryRequest request, CancellationToken cancellationToken) =>
        Ok(await service.GetPagedAsync(request, cancellationToken));

    [HttpGet("{id:int}", Name = "GetCustomerById")]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        FromServiceResult(await service.GetByIdAsync(id, cancellationToken), Ok);

    [HttpGet("{id:int}/orders")]
    [ProducesResponseType(typeof(PagedResponse<CustomerOrderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrdersAsync(int id, [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 20, CancellationToken cancellationToken = default) =>
        FromServiceResult(await service.GetOrdersAsync(id, page, pageSize, cancellationToken), Ok);

    [HttpPost]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateAsync(CustomerRequest request, CancellationToken cancellationToken) =>
        FromServiceResult(await service.CreateAsync(request, cancellationToken),
            item => CreatedAtRoute("GetCustomerById", new { id = item.CustomerId }, item));

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateAsync(int id, CustomerRequest request, CancellationToken cancellationToken) =>
        FromServiceResult(await service.UpdateAsync(id, request, cancellationToken), Ok);

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateAsync(int id, CancellationToken cancellationToken) =>
        FromServiceResult(await service.DeactivateAsync(id, cancellationToken), _ => Ok(new { message = "Customer deactivated." }));
}
