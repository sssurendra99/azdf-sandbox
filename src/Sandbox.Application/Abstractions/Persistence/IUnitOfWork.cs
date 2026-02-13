using System.Data;

namespace Sandbox.Application.Abstractions.Persistence;

public interface IUnitOfWork: IDisposable
{
    // Things to remember here. Unlike EF dapper doesn't have a transactions tracker.
    // Have to track the transactions manually. For this have to use the IDbTransaction interface.

    IDbConnection DbConnection { get; }
    IDbTransaction DbTransaction { get; }

    Task CompleteAsync();
}

