using Microsoft.AspNetCore.Authorization;

namespace DispatchArc.Api.Authorization;

public sealed class TenantAccessRequirement : IAuthorizationRequirement
{
}

public sealed class TenantAccessHandler
    : AuthorizationHandler<TenantAccessRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantAccessHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TenantAccessRequirement requirement)
    {
        var claimTenantId = context.User.FindFirst("tenant_id")?.Value;

        var routeTenantId = _httpContextAccessor.HttpContext?
            .Request.RouteValues["tenantId"]?
            .ToString();

        if (Guid.TryParse(claimTenantId, out var claimId) &&
            Guid.TryParse(routeTenantId, out var routeId) &&
            claimId == routeId)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}