namespace Sandbox.Application.Abstractions.Persistence;

public interface IUnitOfWorkFactory
{
    IUnitOfWorkFactory Create();
}