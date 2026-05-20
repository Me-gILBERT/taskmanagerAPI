using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using TaskManagement.Domain.Interfaces;

namespace TaskManagement.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/analytics")]
public class AnalyticsController : ControllerBase
{
    private readonly IUnitOfWork _uow;

    public AnalyticsController(IUnitOfWork uow)
    {
        _uow = uow;
    }

    [HttpGet("task-summary")]
    public async Task<ActionResult<TaskSummaryResponse>> GetTaskSummary()
    {
        var userId = GetUserId();
        var query = _uow.Tasks.AsQueryable().Where(t => t.UserId == userId);

        var total = await query.CountAsync();
        var byStatus = await query
            .GroupBy(t => t.Status)
            .Select(g => new StatusCount { Status = g.Key.ToString(), Count = g.Count() })
            .ToListAsync();

        return Ok(new TaskSummaryResponse
        {
            TotalTasks = total,
            ByStatus = byStatus
        });
    }

    [HttpGet("completion-rate")]
    public async Task<ActionResult<CompletionRateResponse>> GetCompletionRate([FromQuery] int days = 30)
    {
        var userId = GetUserId();
        var since = DateTime.UtcNow.AddDays(-days);

        var completed = await _uow.Tasks.AsQueryable()
            .Where(t => t.UserId == userId && t.Status == TaskItemStatus.Done && t.CreatedAt >= since)
            .CountAsync();

        var total = await _uow.Tasks.AsQueryable()
            .Where(t => t.UserId == userId && t.CreatedAt >= since)
            .CountAsync();

        var rate = total > 0 ? Math.Round((double)completed / total * 100, 1) : 0;

        return Ok(new CompletionRateResponse
        {
            PeriodDays = days,
            CompletedTasks = completed,
            TotalTasks = total,
            CompletionRatePercent = rate
        });
    }

    [HttpGet("overdue")]
    public async Task<ActionResult<OverdueResponse>> GetOverdue()
    {
        var userId = GetUserId();
        var now = DateTime.UtcNow;

        var overdueTasks = await _uow.Tasks.AsQueryable()
            .Where(t => t.UserId == userId
                && t.Status != TaskItemStatus.Done
                && t.DueDate != null
                && t.DueDate < now)
            .ToListAsync();

        return Ok(new OverdueResponse
        {
            Count = overdueTasks.Count,
            Tasks = overdueTasks.Select(t => new OverdueTask
            {
                Id = t.Id,
                Title = t.Title,
                DueDate = t.DueDate!.Value,
                DaysOverdue = (int)(now - t.DueDate.Value).TotalDays,
                Status = t.Status.ToString()
            }).OrderByDescending(t => t.DaysOverdue).ToList()
        });
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        return Guid.Parse(claim);
    }
}

public record TaskSummaryResponse
{
    public int TotalTasks { get; init; }
    public List<StatusCount> ByStatus { get; init; } = [];
}

public record StatusCount
{
    public string Status { get; init; } = string.Empty;
    public int Count { get; init; }
}

public record CompletionRateResponse
{
    public int PeriodDays { get; init; }
    public int CompletedTasks { get; init; }
    public int TotalTasks { get; init; }
    public double CompletionRatePercent { get; init; }
}

public record OverdueResponse
{
    public int Count { get; init; }
    public List<OverdueTask> Tasks { get; init; } = [];
}

public record OverdueTask
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public DateTime DueDate { get; init; }
    public int DaysOverdue { get; init; }
    public string Status { get; init; } = string.Empty;
}
