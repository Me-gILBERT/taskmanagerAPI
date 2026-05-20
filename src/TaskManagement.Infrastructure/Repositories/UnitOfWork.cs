using Microsoft.EntityFrameworkCore.Storage;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Interfaces;
using TaskManagement.Infrastructure.Data;

namespace TaskManagement.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _db;
    private IRepository<TaskItem>? _tasks;
    private IRepository<User>? _users;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(AppDbContext db)
    {
        _db = db;
    }

    public IRepository<TaskItem> Tasks =>
        _tasks ??= new GenericRepository<TaskItem>(_db);

    public IRepository<User> Users =>
        _users ??= new GenericRepository<User>(_db);

    public async Task<int> SaveChangesAsync() => await _db.SaveChangesAsync();

    public async Task BeginTransactionAsync()
        => _transaction = await _db.Database.BeginTransactionAsync();

    public async Task CommitTransactionAsync()
    {
        if (_transaction != null)
            await _transaction.CommitAsync();
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
            await _transaction.RollbackAsync();
    }

    public void Dispose() => _db.Dispose();
}
