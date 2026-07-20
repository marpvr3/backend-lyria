namespace Lyria.Application.Abstractions.Messaging;

public interface IQuery<out TResponse> : Mediator.IQuery<TResponse>;
