using DispatchArc.Application.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DispatchArc.Api.Controllers;

[ApiController]
[Authorize(Policy = "TenantAccess")]
[Route("api/tenants/{tenantId:guid}/dashboard")]
public sealed class DashboardController : ControllerBase
{
    private readonly DashboardService _dashboardService;

    public DashboardController(
        DashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    [Authorize(Policy = "OwnerOnly")]
    public async Task<ActionResult<DashboardMetricsResponse>> Get(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        try
        {
            var response =
                await _dashboardService.GetAsync(
                    tenantId,
                    cancellationToken);

            return Ok(response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(
                new ProblemDetails
                {
                    Title = "Invalid dashboard request",
                    Detail = exception.Message,
                    Status =
                        StatusCodes.Status400BadRequest
                });
        }
    }
}