using System.Linq.Expressions;

namespace TaskManagement.Domain.Interfaces;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<T>> GetAllAsync();
    Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate);
    IQueryable<T> AsQueryable();
    Task AddAsync(T entity);
    void Update(T entity);
    void Delete(T entity);
}
