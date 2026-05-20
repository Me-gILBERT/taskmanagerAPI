using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Data;

public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditInterceptor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        CaptureAuditLogs(eventData);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        CaptureAuditLogs(eventData);
        return base.SavingChanges(eventData, result);
    }

    private void CaptureAuditLogs(DbContextEventData eventData)
    {
        var context = eventData.Context;
        if (context is null) return;

        var httpContext = _httpContextAccessor.HttpContext;
        var userId = httpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userEmail = httpContext?.User.FindFirstValue(ClaimTypes.Email);

        if (userId is null) return;

        var entries = context.ChangeTracker
            .Entries()
            .Where(e => e.Entity is not AuditLog
                && e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        foreach (var entry in entries)
        {
            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                EntityName = entry.Entity.GetType().Name,
                Action = entry.State.ToString(),
                EntityId = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey())?.CurrentValue?.ToString() ?? "",
                OldValues = entry.State == EntityState.Modified || entry.State == EntityState.Deleted
                    ? JsonSerializer.Serialize(entry.Properties.ToDictionary(p => p.Metadata.Name, p => p.OriginalValue))
                    : null,
                NewValues = entry.State != EntityState.Deleted
                    ? JsonSerializer.Serialize(entry.Properties.ToDictionary(p => p.Metadata.Name, p => p.CurrentValue))
                    : null,
                UserId = Guid.Parse(userId),
                UserEmail = userEmail ?? "",
                CreatedAt = DateTime.UtcNow
            };

            context.Add(auditLog);
        }
    }
}
