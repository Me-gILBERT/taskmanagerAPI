using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Domain.Interfaces;
using TaskManagement.Infrastructure.Data;

namespace TaskManagement.Infrastructure.Repositories;

public class GenericRepository<T> : IRepository<T> where T : class
{
    private readonly AppDbContext _db;
    private readonly DbSet<T> _set;

    public GenericRepository(AppDbContext db)
    {
        _db = db;
        _set = _db.Set<T>();
    }

    public async Task<T?> GetByIdAsync(Guid id) => await _set.FindAsync(id);

    public async Task<IReadOnlyList<T>> GetAllAsync() => await _set.ToListAsync();

    public async Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate)
        => await _set.Where(predicate).ToListAsync();

    public IQueryable<T> AsQueryable() => _set;

    public async Task AddAsync(T entity) => await _set.AddAsync(entity);

    public void Update(T entity) => _set.Update(entity);

    public void Delete(T entity) => _set.Remove(entity);
}
