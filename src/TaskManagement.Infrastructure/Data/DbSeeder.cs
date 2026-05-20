using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (db.Users.IgnoreQueryFilters().Any()) return;

        var passwordHasher = new PasswordHasher<User>();

        var org = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Default Organization",
            CreatedAt = DateTime.UtcNow
        };

        db.Organizations.Add(org);

        var admin = new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@taskmanager.com",
            Username = "admin",
            PasswordHash = passwordHasher.HashPassword(null!, "Admin123!"),
            Role = UserRole.Admin,
            CreatedAt = DateTime.UtcNow,
            OrganizationId = org.Id
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "user@taskmanager.com",
            Username = "user",
            PasswordHash = passwordHasher.HashPassword(null!, "User123!"),
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow,
            OrganizationId = org.Id
        };

        db.Users.AddRange(admin, user);

        var tasks = new List<TaskItem>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Set up project infrastructure",
                Description = "Configure Docker, CI/CD, and database",
                Status = TaskItemStatus.Done,
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                DueDate = DateTime.UtcNow.AddDays(-2),
                UserId = admin.Id,
                OrganizationId = org.Id
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Implement authentication",
                Description = "Add JWT-based authentication with refresh tokens",
                Status = TaskItemStatus.InProgress,
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                DueDate = DateTime.UtcNow.AddDays(5),
                UserId = admin.Id,
                OrganizationId = org.Id
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Write API documentation",
                Description = "Document all endpoints with OpenAPI/Swagger",
                Status = TaskItemStatus.Todo,
                CreatedAt = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddDays(10),
                UserId = user.Id,
                OrganizationId = org.Id
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Add caching layer",
                Description = "Implement Redis distributed caching for performance",
                Status = TaskItemStatus.Todo,
                CreatedAt = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddDays(14),
                UserId = user.Id,
                OrganizationId = org.Id
            }
        };

        db.Tasks.AddRange(tasks);
        await db.SaveChangesAsync();
    }
}
