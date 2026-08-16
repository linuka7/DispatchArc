using DispatchArc.Application.Alerts;
using DispatchArc.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DispatchArc.Api.Controllers;

[ApiController]
[Authorize(Policy = "TenantAccess")]
[Authorize(Policy = "OperationalAlertsAccess")]
[Route("api/tenants/{tenantId:guid}/alerts")]
public sealed class OperationalAlertsController
    : ControllerBase
{
    private readonly OperationalAlertService _alerts;

    public OperationalAlertsController(
        OperationalAlertService alerts)
    {
        _alerts = alerts;
    }

    [HttpGet]
    public async Task<ActionResult<OperationalAlertFeedResponse>> Get(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var audience =
            ResolveAudience();

        if (!audience.HasValue)
        {
            return Forbid();
        }

        try
        {
            var response =
                await _alerts.GetAsync(
                    tenantId,
                    audience.Value,
                    cancellationToken);

            return Ok(response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(
                new ProblemDetails
                {
                    Title =
                        "Invalid operational alert request",
                    Detail =
                        exception.Message,
                    Status =
                        StatusCodes.Status400BadRequest
                });
        }
    }

    private OperationalAlertAudience? ResolveAudience()
    {
        if (User.IsInRole(
            nameof(UserRole.Owner)))
        {
            return OperationalAlertAudience.All;
        }

        if (User.IsInRole(
            nameof(UserRole.Dispatcher)))
        {
            return OperationalAlertAudience.Operations;
        }

        if (User.IsInRole(
            nameof(UserRole.Finance)))
        {
            return OperationalAlertAudience.Finance;
        }

        return null;
    }
}