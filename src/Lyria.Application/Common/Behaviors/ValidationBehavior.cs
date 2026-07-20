using System.Linq.Expressions;
using FluentValidation;
using FluentValidation.Results;
using Lyria.Application.Common.Errors;
using Lyria.Application.Common.Results;
using Mediator;

namespace Lyria.Application.Common.Behaviors;

public sealed class ValidationBehavior<TMessage, TResponse>(
    IEnumerable<IValidator<TMessage>> validators)
    : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
        {
            return await next(message, cancellationToken);
        }

        var context = new ValidationContext<TMessage>(message);

        ValidationResult[] validationResults = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        List<ValidationFailure> failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count == 0)
        {
            return await next(message, cancellationToken);
        }

        string description = string.Join("; ", failures.Select(f => f.ErrorMessage));
        var error = Error.Validation("General.Validation", description);

        return FailureFactory.Create(error);
    }

    private static class FailureFactory
    {
        private static readonly Func<Error, TResponse> Factory = CompileFactory();

        public static TResponse Create(Error error) => Factory(error);

        private static Func<Error, TResponse> CompileFactory()
        {
            if (typeof(TResponse) == typeof(Result))
            {
                return error => (TResponse)(object)Result.Failure(error);
            }

            if (typeof(TResponse).IsGenericType &&
                typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
            {
                ParameterExpression errorParam = Expression.Parameter(typeof(Error), "error");

                MethodCallExpression call = Expression.Call(
                    typeof(Result),
                    nameof(Result.Failure),
                    typeof(TResponse).GetGenericArguments(),
                    errorParam);

                return Expression.Lambda<Func<Error, TResponse>>(call, errorParam).Compile();
            }

            return _ => throw new InvalidOperationException(
                "La validación solo es compatible con comandos que retornan Result o Result<T>.");
        }
    }
}
