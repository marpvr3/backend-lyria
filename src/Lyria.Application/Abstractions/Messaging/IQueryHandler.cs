namespace Lyria.Application.Abstractions.Messaging;

public interface IQueryHandler<in TQuery, TResponse>
    : Mediator.IQueryHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>;
