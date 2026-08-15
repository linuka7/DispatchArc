using Microsoft.AspNetCore.Authorization;
using DispatchArc.Api.Contracts.Customers;
using DispatchArc.Application.Customers;
using Microsoft.AspNetCore.Mvc;

namespace DispatchArc.Api.Controllers;

[ApiController]
[Authorize(Policy = "TenantAccess")]
[Route("api/tenants/{tenantId:guid}/customers")]
public sealed class CustomersController : ControllerBase
{
    private readonly CustomerService _customerService;

    public CustomersController(CustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpPost]
    [Authorize(Policy = "DispatchManagement")]
    public async Task<ActionResult<CustomerResponse>> Create(
        Guid tenantId,
        CreateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var customer = await _customerService.CreateAsync(
            tenantId,
            request.Name,
            request.Phone,
            request.Email,
            request.AddressLine,
            request.City,
            cancellationToken);

        if (customer is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Tenant not found",
                Detail = "The requested business tenant does not exist or is inactive.",
                Status = StatusCodes.Status404NotFound
            });
        }

        return CreatedAtAction(
            nameof(GetById),
            new
            {
                tenantId,
                customerId = customer.Id
            },
            customer);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CustomerResponse>>> GetAll(
        Guid tenantId,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var customers = await _customerService.GetAllAsync(
            tenantId,
            search,
            cancellationToken);

        return Ok(customers);
    }

    [HttpGet("{customerId:guid}")]
    public async Task<ActionResult<CustomerResponse>> GetById(
        Guid tenantId,
        Guid customerId,
        CancellationToken cancellationToken)
    {
        var customer = await _customerService.GetByIdAsync(
            tenantId,
            customerId,
            cancellationToken);

        return customer is null ? NotFound() : Ok(customer);
    }
}


