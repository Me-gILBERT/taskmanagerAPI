using FluentAssertions;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Tests.Unit;

public class TaskItemTests
{
    [Fact]
    public void CreateTask_ShouldHaveCorrectDefaults()
    {
        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = "Test Task",
            Status = TaskItemStatus.Todo,
            CreatedAt = DateTime.UtcNow
        };

        task.Title.Should().Be("Test Task");
        task.Status.Should().Be(TaskItemStatus.Todo);
        task.Description.Should().BeNull();
    }

    [Theory]
    [InlineData(TaskItemStatus.Todo)]
    [InlineData(TaskItemStatus.InProgress)]
    [InlineData(TaskItemStatus.Done)]
    public void TaskStatus_ShouldHaveValidValues(TaskItemStatus status)
    {
        status.Should().BeOneOf(TaskItemStatus.Todo, TaskItemStatus.InProgress, TaskItemStatus.Done);
    }
}
