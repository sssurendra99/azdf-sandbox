namespace Sandbox.Application.Abstractions.Persistence;

public interface IGenericRepository<T> where T : class
{
    
    Task<T?> GetByIdAsync(Guid id);
    Task<T?> GetByIdAsync(int id);
    Task<T?> GetByIdAsync(string id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> FindAsync(string whereClause, object? parameters = null);
    Task<T?> FirstOrDefaultAsync(string whereClause, object? parameters = null);
    
    Task<Guid> InsertAsync(T entity);
    Task<int> InsertRangeAsync(IEnumerable<T> entities);
    
    Task<bool> UpdateAsync(T entity);
    Task<int> UpdateRangeAsync(IEnumerable<T> entities);
    
    Task<bool> DeleteAsync(Guid id);
    Task<bool> DeleteAsync(int id);
    Task<bool> DeleteAsync(string id);
    Task<int> DeleteRangeAsync(IEnumerable<Guid> ids);
    
    Task<bool> ExistsAsync(Guid id);
    Task<bool> ExistsAsync(string whereClause, object? parameters = null);
    Task<int> CountAsync(string? whereClause = null, object? parameters = null);
    Task<IEnumerable<T>> GetPagedAsync(int pageNumber, int pageSize, string? orderBy = null);
    
    Task<IEnumerable<T>> QueryAsync(string sql, object? parameters = null);
    Task<T?> QueryFirstOrDefaultAsync(string sql, object? parameters = null);
    Task<int> ExecuteAsync(string sql, object? parameters = null);
}