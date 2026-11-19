using Prepround.Domain.Shared;

namespace Prepround.Application.Abstractions.Messaging;

public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken);
}
