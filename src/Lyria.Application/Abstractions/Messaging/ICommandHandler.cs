using Lyria.Application.Common.Results;

namespace Lyria.Application.Abstractions.Messaging;

public interface ICommandHandler<in TCommand>
    : Mediator.ICommandHandler<TCommand, Result>
    where TCommand : ICommand;

public interface ICommandHandler<in TCommand, TResponse>
    : Mediator.ICommandHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse>;
