namespace Sandbox.Application.Abstractions.Persistence;

public interface IUnitOfWorkFactory
{
    IUnitOfWork Create();
}