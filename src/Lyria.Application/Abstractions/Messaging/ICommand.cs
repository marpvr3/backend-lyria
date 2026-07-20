using Lyria.Application.Common.Results;

namespace Lyria.Application.Abstractions.Messaging;

public interface ICommand : Mediator.ICommand<Result>;

public interface ICommand<TResponse> : Mediator.ICommand<Result<TResponse>>;
