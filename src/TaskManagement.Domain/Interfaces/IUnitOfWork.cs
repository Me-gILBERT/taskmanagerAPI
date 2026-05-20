using TaskManagement.Domain.Entities;

namespace TaskManagement.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IRepository<TaskItem> Tasks { get; }
    IRepository<User> Users { get; }
    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
