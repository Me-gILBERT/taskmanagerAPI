using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TaskManagement.API.Exceptions;
using TaskManagement.API.Hubs;
using TaskManagement.Application.DTOs;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using TaskManagement.Domain.Interfaces;

namespace TaskManagement.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/tasks")]
public class TasksController : ControllerBase
{
    private readonly IUnitOfWork _uow;
    private readonly IHubContext<TaskHub> _hubContext;

    public TasksController(IUnitOfWork uow, IHubContext<TaskHub> hubContext)
    {
        _uow = uow;
        _hubContext = hubContext;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<TaskItem>>> GetAll([FromQuery] TaskQueryParameters query)
    {
        var userId = GetUserId();
        var userTasks = _uow.Tasks.AsQueryable().Where(t => t.UserId == userId);

        if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<TaskItemStatus>(query.Status, true, out var status))
            userTasks = userTasks.Where(t => t.Status == status);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.ToLower();
            userTasks = userTasks.Where(t =>
                t.Title.ToLower().Contains(search) ||
                (t.Description != null && t.Description.ToLower().Contains(search)));
        }

        if (query.DueDateFrom.HasValue)
            userTasks = userTasks.Where(t => t.DueDate >= query.DueDateFrom);

        if (query.DueDateTo.HasValue)
            userTasks = userTasks.Where(t => t.DueDate <= query.DueDateTo);

        userTasks = (query.SortBy.ToLower(), query.SortOrder.ToLower()) switch
        {
            ("title", "asc") => userTasks.OrderBy(t => t.Title),
            ("title", "desc") => userTasks.OrderByDescending(t => t.Title),
            ("status", "asc") => userTasks.OrderBy(t => t.Status),
            ("status", "desc") => userTasks.OrderByDescending(t => t.Status),
            ("duedate", "asc") => userTasks.OrderBy(t => t.DueDate),
            ("duedate", "desc") => userTasks.OrderByDescending(t => t.DueDate),
            ("createdat", "asc") => userTasks.OrderBy(t => t.CreatedAt),
            _ => userTasks.OrderByDescending(t => t.CreatedAt)
        };

        var totalRecords = await userTasks.CountAsync();
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var pageNumber = Math.Max(query.PageNumber, 1);
        var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

        var data = await userTasks
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        Response.Headers["X-Total-Count"] = totalRecords.ToString();

        return Ok(new PagedResult<TaskItem>
        {
            Data = data,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = totalPages,
            TotalRecords = totalRecords
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TaskItem>> GetById(Guid id)
    {
        var userId = GetUserId();
        var task = await _uow.Tasks.GetByIdAsync(id);
        if (task is null || task.UserId != userId) return NotFound();
        return task;
    }

    [HttpPost]
    public async Task<ActionResult<TaskItem>> Create(CreateTaskRequest request, [FromServices] ITenantService tenantService)
    {
        var userId = GetUserId();
        var tenantId = tenantService.GetTenantId() ?? throw new BusinessException("MISSING_TENANT", "Tenant not found");
        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            Status = TaskItemStatus.Todo,
            CreatedAt = DateTime.UtcNow,
            DueDate = EnsureUtc(request.DueDate),
            UserId = userId,
            OrganizationId = tenantId
        };
        await _uow.Tasks.AddAsync(task);
        await _uow.SaveChangesAsync();
        await _hubContext.Clients.Group(userId.ToString()).SendAsync("TaskCreated", task);
        return CreatedAtAction(nameof(GetById), new { id = task.Id }, task);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TaskItem>> Update(Guid id, UpdateTaskRequest request)
    {
        var userId = GetUserId();
        var task = await _uow.Tasks.GetByIdAsync(id);
        if (task is null || task.UserId != userId) return NotFound();

        task.Title = request.Title;
        task.Description = request.Description;
        task.Status = request.Status;
        task.DueDate = EnsureUtc(request.DueDate);
        _uow.Tasks.Update(task);
        await _uow.SaveChangesAsync();
        await _hubContext.Clients.Group(userId.ToString()).SendAsync("TaskUpdated", task);
        return task;
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = GetUserId();
        var task = await _uow.Tasks.GetByIdAsync(id);
        if (task is null || task.UserId != userId) return NotFound();
        _uow.Tasks.Delete(task);
        await _uow.SaveChangesAsync();
        await _hubContext.Clients.Group(userId.ToString()).SendAsync("TaskDeleted", new { id });
        return NoContent();
    }

    private static DateTime? EnsureUtc(DateTime? dt) =>
        dt.HasValue ? DateTime.SpecifyKind(dt.Value, DateTimeKind.Utc) : null;

    private Guid GetUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        return Guid.Parse(claim);
    }
}

public record CreateTaskRequest(string Title, string? Description, DateTime? DueDate);

public record UpdateTaskRequest(string Title, string? Description, TaskItemStatus Status, DateTime? DueDate);
