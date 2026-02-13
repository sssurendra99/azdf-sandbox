using System.Data;
using Sandbox.Application.Abstractions.Persistence;
using Microsoft.Data.SqlClient;

namespace Sandbox.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{

    private readonly IDbConnection dbConnection;
    private readonly IDbTransaction dbTransaction;
    private readonly bool isCompleted;

    public IDbConnection DbConnection => dbConnection;
    public IDbTransaction DbTransaction =>  dbTransaction;

    public UnitOfWork(string dbConnectionString)
    {
        dbConnection = new SqlConnection(dbConnectionString);
        dbConnection.Open();

        dbTransaction = dbConnection.BeginTransaction();
    }

    public Task CompleteAsync()
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}