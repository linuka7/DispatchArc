using DispatchArc.Api.Contracts.Tenants;
using DispatchArc.Application.Tenants;
using Microsoft.AspNetCore.Mvc;

namespace DispatchArc.Api.Controllers;

[ApiController]
[Route("api/tenants")]
public sealed class TenantsController : ControllerBase
{
    private readonly TenantService _tenantService;

    public TenantsController(TenantService tenantService)
    {
        _tenantService = tenantService;
    }

    [HttpPost]
    public async Task<ActionResult<TenantResponse>> Create(
        CreateTenantRequest request,
        CancellationToken cancellationToken)
    {
        var tenant = await _tenantService.CreateAsync(
            request.Name,
            request.Slug,
            cancellationToken);

        if (tenant is null)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Tenant slug already exists",
                Detail = $"The slug '{request.Slug}' is already registered.",
                Status = StatusCodes.Status409Conflict
            });
        }

        return CreatedAtAction(
            nameof(GetById),
            new { tenantId = tenant.Id },
            tenant);
    }

    [HttpGet("{tenantId:guid}")]
    public async Task<ActionResult<TenantResponse>> GetById(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var tenant = await _tenantService.GetByIdAsync(
            tenantId,
            cancellationToken);

        return tenant is null ? NotFound() : Ok(tenant);
    }
}