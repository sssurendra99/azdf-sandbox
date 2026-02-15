using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Reflection;
using System.Text;
using Dapper;

using Sandbox.Application.Abstractions.Persistence;

namespace Sandbox.Infrastructure.Persistence;

public class GenericRepository<T> : IGenericRepository<T> where T : class
{
    protected readonly IUnitOfWork _unitOfWork;
    protected readonly string _tableName;
    protected readonly string _primaryKeyName;
    protected readonly Type _primaryKeyType;

    public GenericRepository(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
        _tableName = GetTableName();
        (_primaryKeyName, _primaryKeyType) = GetPrimaryKey();
    }

    #region Metadata Helpers

    private string GetTableName()
    {
        var tableAttribute = typeof(T).GetCustomAttribute<TableAttribute>();
        return tableAttribute?.Name ?? $"{typeof(T).Name}s";
    }

    private (string Name, Type Type) GetPrimaryKey()
    {
        var properties = typeof(T).GetProperties();

        // Look for [Key] attribute
        var keyProperty = properties.FirstOrDefault(p => p.GetCustomAttribute<KeyAttribute>() != null);

        // If not found, look for "Id" property
        if (keyProperty == null)
        {
            keyProperty = properties.FirstOrDefault(p => 
                p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase));
        }

        if (keyProperty == null)
            throw new InvalidOperationException($"Entity {typeof(T).Name} does not have a primary key property.");

        return (keyProperty.Name, keyProperty.PropertyType);
    }

    private IEnumerable<PropertyInfo> GetProperties()
    {
        return typeof(T).GetProperties()
            .Where(p => p.CanRead && p.CanWrite)
            .Where(p => !p.GetCustomAttributes<NotMappedAttribute>().Any());
    }

    private IEnumerable<PropertyInfo> GetInsertProperties()
    {
        return GetProperties()
            .Where(p => p.Name != _primaryKeyName || _primaryKeyType != typeof(Guid))
            .Where(p => p.Name != "UpdatedAt");
    }

    private IEnumerable<PropertyInfo> GetUpdateProperties()
    {
        return GetProperties()
            .Where(p => p.Name != _primaryKeyName)
            .Where(p => p.Name != "CreatedAt");
    }

    #endregion

    #region Get Operations

    public async Task<T?> GetByIdAsync(Guid id)
    {
        var sql = $"SELECT * FROM {_tableName} WHERE {_primaryKeyName} = @Id";
        return await _unitOfWork.DbConnection.QueryFirstOrDefaultAsync<T>(
            sql, 
            new { Id = id }, 
            _unitOfWork.DbTransaction);
    }

    public async Task<T?> GetByIdAsync(int id)
    {
        var sql = $"SELECT * FROM {_tableName} WHERE {_primaryKeyName} = @Id";
        return await _unitOfWork.DbConnection.QueryFirstOrDefaultAsync<T>(
            sql, 
            new { Id = id }, 
            _unitOfWork.DbTransaction);
    }

    public async Task<T?> GetByIdAsync(string id)
    {
        var sql = $"SELECT * FROM {_tableName} WHERE {_primaryKeyName} = @Id";
        return await _unitOfWork.DbConnection.QueryFirstOrDefaultAsync<T>(
            sql, 
            new { Id = id }, 
            _unitOfWork.DbTransaction);
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        var sql = $"SELECT * FROM {_tableName}";
        return await _unitOfWork.DbConnection.QueryAsync<T>(
            sql, 
            transaction: _unitOfWork.DbTransaction);
    }

    public async Task<IEnumerable<T>> FindAsync(string whereClause, object? parameters = null)
    {
        var sql = $"SELECT * FROM {_tableName} WHERE {whereClause}";
        return await _unitOfWork.DbConnection.QueryAsync<T>(
            sql, 
            parameters, 
            _unitOfWork.DbTransaction);
    }

    public async Task<T?> FirstOrDefaultAsync(string whereClause, object? parameters = null)
    {
        var sql = $"SELECT TOP 1 * FROM {_tableName} WHERE {whereClause}";
        return await _unitOfWork.DbConnection.QueryFirstOrDefaultAsync<T>(
            sql, 
            parameters, 
            _unitOfWork.DbTransaction);
    }

    #endregion

    #region Insert Operations

    public async Task<Guid> InsertAsync(T entity)
    {
        var properties = GetInsertProperties();
        var columns = string.Join(", ", properties.Select(p => p.Name));
        var values = string.Join(", ", properties.Select(p => $"@{p.Name}"));

        var sql = $"INSERT INTO {_tableName} ({columns}) VALUES ({values})";

        // If primary key is GUID, generate it
        if (_primaryKeyType == typeof(Guid))
        {
            var pkProperty = typeof(T).GetProperty(_primaryKeyName);
            var currentValue = pkProperty?.GetValue(entity);
            
            if (currentValue == null || (Guid)currentValue == Guid.Empty)
            {
                var newId = Guid.NewGuid();
                pkProperty?.SetValue(entity, newId);
            }

            await _unitOfWork.DbConnection.ExecuteAsync(
                sql, 
                entity, 
                _unitOfWork.DbTransaction);

            return (Guid)pkProperty!.GetValue(entity)!;
        }
        else if (_primaryKeyType == typeof(int))
        {
            // For INT IDENTITY, use OUTPUT clause
            sql = $@"
                INSERT INTO {_tableName} ({columns}) 
                OUTPUT INSERTED.{_primaryKeyName}
                VALUES ({values})";

            var id = await _unitOfWork.DbConnection.ExecuteScalarAsync<int>(
                sql, 
                entity, 
                _unitOfWork.DbTransaction);

            return Guid.Parse(id.ToString());
        }
        else
        {
            await _unitOfWork.DbConnection.ExecuteAsync(
                sql, 
                entity, 
                _unitOfWork.DbTransaction);

            return Guid.Empty;
        }
    }

    public async Task<int> InsertRangeAsync(IEnumerable<T> entities)
    {
        var properties = GetInsertProperties();
        var columns = string.Join(", ", properties.Select(p => p.Name));
        var values = string.Join(", ", properties.Select(p => $"@{p.Name}"));

        var sql = $"INSERT INTO {_tableName} ({columns}) VALUES ({values})";

        // Generate GUIDs if needed
        if (_primaryKeyType == typeof(Guid))
        {
            var pkProperty = typeof(T).GetProperty(_primaryKeyName);
            foreach (var entity in entities)
            {
                var currentValue = pkProperty?.GetValue(entity);
                if (currentValue == null || (Guid)currentValue == Guid.Empty)
                {
                    pkProperty?.SetValue(entity, Guid.NewGuid());
                }
            }
        }

        return await _unitOfWork.DbConnection.ExecuteAsync(
            sql, 
            entities, 
            _unitOfWork.DbTransaction);
    }

    #endregion

    #region Update Operations

    public async Task<bool> UpdateAsync(T entity)
    {
        var properties = GetUpdateProperties();
        var setClause = string.Join(", ", properties.Select(p => $"{p.Name} = @{p.Name}"));

        var sql = $"UPDATE {_tableName} SET {setClause} WHERE {_primaryKeyName} = @{_primaryKeyName}";

        var rowsAffected = await _unitOfWork.DbConnection.ExecuteAsync(
            sql, 
            entity, 
            _unitOfWork.DbTransaction);

        return rowsAffected > 0;
    }

    public async Task<int> UpdateRangeAsync(IEnumerable<T> entities)
    {
        var properties = GetUpdateProperties();
        var setClause = string.Join(", ", properties.Select(p => $"{p.Name} = @{p.Name}"));

        var sql = $"UPDATE {_tableName} SET {setClause} WHERE {_primaryKeyName} = @{_primaryKeyName}";

        return await _unitOfWork.DbConnection.ExecuteAsync(
            sql, 
            entities, 
            _unitOfWork.DbTransaction);
    }

    #endregion

    #region Delete Operations

    public async Task<bool> DeleteAsync(Guid id)
    {
        var sql = $"DELETE FROM {_tableName} WHERE {_primaryKeyName} = @Id";
        var rowsAffected = await _unitOfWork.DbConnection.ExecuteAsync(
            sql, 
            new { Id = id }, 
            _unitOfWork.DbTransaction);
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var sql = $"DELETE FROM {_tableName} WHERE {_primaryKeyName} = @Id";
        var rowsAffected = await _unitOfWork.DbConnection.ExecuteAsync(
            sql, 
            new { Id = id }, 
            _unitOfWork.DbTransaction);
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var sql = $"DELETE FROM {_tableName} WHERE {_primaryKeyName} = @Id";
        var rowsAffected = await _unitOfWork.DbConnection.ExecuteAsync(
            sql, 
            new { Id = id }, 
            _unitOfWork.DbTransaction);
        return rowsAffected > 0;
    }

    public async Task<int> DeleteRangeAsync(IEnumerable<Guid> ids)
    {
        var sql = $"DELETE FROM {_tableName} WHERE {_primaryKeyName} IN @Ids";
        return await _unitOfWork.DbConnection.ExecuteAsync(
            sql, 
            new { Ids = ids }, 
            _unitOfWork.DbTransaction);
    }

    #endregion

    #region Additional Operations

    public async Task<bool> ExistsAsync(Guid id)
    {
        var sql = $"SELECT COUNT(1) FROM {_tableName} WHERE {_primaryKeyName} = @Id";
        var count = await _unitOfWork.DbConnection.ExecuteScalarAsync<int>(
            sql, 
            new { Id = id }, 
            _unitOfWork.DbTransaction);
        return count > 0;
    }

    public async Task<bool> ExistsAsync(string whereClause, object? parameters = null)
    {
        var sql = $"SELECT COUNT(1) FROM {_tableName} WHERE {whereClause}";
        var count = await _unitOfWork.DbConnection.ExecuteScalarAsync<int>(
            sql, 
            parameters, 
            _unitOfWork.DbTransaction);
        return count > 0;
    }

    public async Task<int> CountAsync(string? whereClause = null, object? parameters = null)
    {
        var sql = string.IsNullOrEmpty(whereClause)
            ? $"SELECT COUNT(1) FROM {_tableName}"
            : $"SELECT COUNT(1) FROM {_tableName} WHERE {whereClause}";

        return await _unitOfWork.DbConnection.ExecuteScalarAsync<int>(
            sql, 
            parameters, 
            _unitOfWork.DbTransaction);
    }

    public async Task<IEnumerable<T>> GetPagedAsync(int pageNumber, int pageSize, string? orderBy = null)
    {
        var offset = (pageNumber - 1) * pageSize;
        var orderByClause = string.IsNullOrEmpty(orderBy) ? _primaryKeyName : orderBy;

        var sql = $@"
            SELECT * FROM {_tableName}
            ORDER BY {orderByClause}
            OFFSET @Offset ROWS
            FETCH NEXT @PageSize ROWS ONLY";

        return await _unitOfWork.DbConnection.QueryAsync<T>(
            sql,
            new { Offset = offset, PageSize = pageSize },
            _unitOfWork.DbTransaction);
    }

    #endregion

    #region Raw SQL Operations

    public async Task<IEnumerable<T>> QueryAsync(string sql, object? parameters = null)
    {
        return await _unitOfWork.DbConnection.QueryAsync<T>(
            sql, 
            parameters, 
            _unitOfWork.DbTransaction);
    }

    public async Task<T?> QueryFirstOrDefaultAsync(string sql, object? parameters = null)
    {
        return await _unitOfWork.DbConnection.QueryFirstOrDefaultAsync<T>(
            sql, 
            parameters, 
            _unitOfWork.DbTransaction);
    }

    public async Task<int> ExecuteAsync(string sql, object? parameters = null)
    {
        return await _unitOfWork.DbConnection.ExecuteAsync(
            sql, 
            parameters, 
            _unitOfWork.DbTransaction);
    }

    #endregion
}
