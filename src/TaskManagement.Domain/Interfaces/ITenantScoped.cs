namespace TaskManagement.Domain.Interfaces;

public interface ITenantScoped
{
    Guid OrganizationId { get; set; }
}
