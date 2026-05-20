using TaskManagement.Domain.Enums;
using TaskManagement.Domain.Interfaces;

namespace TaskManagement.Domain.Entities;

public class TaskItem : ITenantScoped
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskItemStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DueDate { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid OrganizationId { get; set; }
}
