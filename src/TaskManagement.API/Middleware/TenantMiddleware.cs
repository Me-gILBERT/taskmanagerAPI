using System.Security.Claims;

namespace TaskManagement.API.Middleware;

public class TenantMiddleware
{
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var orgClaim = context.User.FindFirst("organizationId");
            if (orgClaim is not null && Guid.TryParse(orgClaim.Value, out var tenantId))
                context.Items["TenantId"] = tenantId;
        }

        await _next(context);
    }
}
