using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DispatchArc.Application.Auth;
using DispatchArc.Domain.Enums;

namespace DispatchArc.Api.Controllers;

[ApiController]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[Authorize(Policy = "TenantAccess")]
[Route("api/tenants/{tenantId:guid}/technicians")]
public sealed class TechniciansController : ControllerBase
{
    private readonly IAppUserRepository _users;

    public TechniciansController(IAppUserRepository users)
    {
        _users = users;
    }

    [HttpGet]
    [Authorize(Policy = "DispatchManagement")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var users = await _users.ListByTenantAsync(
            tenantId,
            cancellationToken);

        var technicians = users
            .Where(user =>
                user.Role == UserRole.Technician &&
                user.IsActive)
            .Select(user => new
            {
                id = user.Id,
                fullName = user.FullName,
                email = user.Email
            })
            .ToList();

        return Ok(technicians);
    }
}