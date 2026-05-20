using TaskManagement.Domain.Interfaces;

namespace TaskManagement.API.Services;

public class TenantService : ITenantService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? GetTenantId()
    {
        var context = _httpContextAccessor.HttpContext;
        return context?.Items["TenantId"] as Guid?;
    }
}
