using Microsoft.Extensions.Configuration;
using Sandbox.Application.Abstractions.Persistence;

namespace Sandbox.Infrastructure.Persistence;
public class UnitOfWorkFactory : IUnitOfWorkFactory
{
    private readonly string _connectionString;

    public UnitOfWorkFactory(IConfiguration configuration)
    {
        _connectionString = configuration["SqlConnectionString"]
            ?? throw new ArgumentNullException("SqlConnectionString not configured");
    }

    public IUnitOfWork Create()
    {
        return new UnitOfWork(_connectionString);
    }
}
