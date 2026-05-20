using Microsoft.EntityFrameworkCore.Diagnostics;
using TaskManagement.Domain.Interfaces;

namespace TaskManagement.Infrastructure.Data;

public class TenantInterceptor : SaveChangesInterceptor
{
    private readonly Guid _tenantId;

    public TenantInterceptor(Guid tenantId)
    {
        _tenantId = tenantId;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        SetTenantId(eventData);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        SetTenantId(eventData);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void SetTenantId(DbContextEventData eventData)
    {
        foreach (var entry in eventData.Context!.ChangeTracker.Entries<ITenantScoped>())
        {
            if (entry.State == Microsoft.EntityFrameworkCore.EntityState.Added)
                entry.Entity.OrganizationId = _tenantId;
        }
    }
}
