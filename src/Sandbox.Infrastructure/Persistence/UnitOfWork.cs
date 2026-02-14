using System.Data;
using Sandbox.Application.Abstractions.Persistence;
using Microsoft.Data.SqlClient;

namespace Sandbox.Infrastructure.Persistence;
public class UnitOfWork : IUnitOfWork
{
    private readonly IDbConnection _dbConnection;
    private IDbTransaction? _dbTransaction;
    private bool _isCompleted;
    private bool _disposed;

    public IDbConnection DbConnection => _dbConnection;
    public IDbTransaction DbTransaction => _dbTransaction ?? throw new InvalidOperationException("Transaction not started");

    public UnitOfWork(string dbConnectionString)
    {
        if (string.IsNullOrWhiteSpace(dbConnectionString))
            throw new ArgumentNullException(nameof(dbConnectionString));

        _dbConnection = new SqlConnection(dbConnectionString);
        _dbConnection.Open();
        _dbTransaction = _dbConnection.BeginTransaction();
    }

    public async Task CompleteAsync()
    {
        if (_isCompleted)
            throw new InvalidOperationException("Transaction already completed");

        if (_dbTransaction == null)
            throw new InvalidOperationException("Transaction not started");

        try
        {
            _dbTransaction.Commit();
            _isCompleted = true;
        }
        catch
        {
            _dbTransaction.Rollback();
            throw;
        }
        finally
        {
            _dbTransaction.Dispose();
            _dbTransaction = null;
        }

        await Task.CompletedTask;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // If transaction was not completed, roll it back
                if (_dbTransaction != null && !_isCompleted)
                {
                    _dbTransaction.Rollback();
                    _dbTransaction.Dispose();
                    _dbTransaction = null;
                }

                _dbConnection?.Dispose();
            }

            _disposed = true;
        }
    }
}
