using Sandbox.Application.Abstractions.Persistence;

namespace Sandbox.Infrastructure.Persistence;

public class UnitOfWorkFactory : IUnitOfWorkFactory
{
    private readonly string _connectionString;

    public UnitOfWorkFactory(string connectionString)
    {
        _connectionString = connectionString
            ?? throw new ArgumentNullException(nameof(connectionString), "SQL connection string not configured");
    }

    public IUnitOfWork Create()
    {
        return new UnitOfWork(_connectionString);
    }
}
