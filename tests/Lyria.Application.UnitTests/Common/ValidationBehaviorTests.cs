using FluentValidation;
using FluentValidation.Results;
using Lyria.Application.Common.Behaviors;
using Lyria.Application.Common.Errors;
using Lyria.Application.Common.Results;
using Lyria.Application.Features.EstablishmentCategories.Create;
using Lyria.Application.Features.EstablishmentCategories.Deactivate;
using Lyria.Domain.Establishments.Categories;
using Mediator;
using Xunit;

namespace Lyria.Application.UnitTests.Common;

public class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_ValidationFails_HandlerNotExecuted_Result()
    {
        var validator = new InlineValidator<DeactivateEstablishmentCategoryCommand>();
        validator.RuleFor(x => x.Id).Must(_ => false).WithMessage("Id inválido");

        var behavior = new ValidationBehavior<DeactivateEstablishmentCategoryCommand, Result>(
            [validator]);

        bool handlerCalled = false;
        MessageHandlerDelegate<DeactivateEstablishmentCategoryCommand, Result> next =
            (_, _) =>
            {
                handlerCalled = true;
                return new ValueTask<Result>(Result.Success());
            };

        var command = new DeactivateEstablishmentCategoryCommand(Guid.NewGuid());

        Result result = await behavior.Handle(
            command, next, TestContext.Current.CancellationToken);

        Assert.False(handlerCalled);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_ValidationFails_HandlerNotExecuted_ResultT()
    {
        var validator = new InlineValidator<CreateEstablishmentCategoryCommand>();
        validator.RuleFor(x => x.Name).Must(_ => false).WithMessage("Nombre inválido");

        var behavior =
            new ValidationBehavior<CreateEstablishmentCategoryCommand, Result<EstablishmentCategoryId>>(
                [validator]);

        bool handlerCalled = false;
        MessageHandlerDelegate<CreateEstablishmentCategoryCommand, Result<EstablishmentCategoryId>> next =
            (_, _) =>
            {
                handlerCalled = true;
                return new ValueTask<Result<EstablishmentCategoryId>>(
                    Result.Success(EstablishmentCategoryId.New()));
            };

        var command = new CreateEstablishmentCategoryCommand("Nombre", null, null, 1);

        Result<EstablishmentCategoryId> result = await behavior.Handle(
            command, next, TestContext.Current.CancellationToken);

        Assert.False(handlerCalled);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_ValidationFails_ReturnsValidationErrorType()
    {
        var validator = new InlineValidator<DeactivateEstablishmentCategoryCommand>();
        validator.RuleFor(x => x.Id).Must(_ => false).WithMessage("Error de prueba");

        var behavior = new ValidationBehavior<DeactivateEstablishmentCategoryCommand, Result>(
            [validator]);

        MessageHandlerDelegate<DeactivateEstablishmentCategoryCommand, Result> next =
            (_, _) => new ValueTask<Result>(Result.Success());

        var command = new DeactivateEstablishmentCategoryCommand(Guid.NewGuid());

        Result result = await behavior.Handle(
            command, next, TestContext.Current.CancellationToken);

        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("General.Validation", result.Error.Code);
    }

    [Fact]
    public async Task Handle_MultipleValidationErrors_AllIncludedInDescription()
    {
        var validator1 = new InlineValidator<CreateEstablishmentCategoryCommand>();
        validator1.RuleFor(x => x.Name).Must(_ => false).WithMessage("El nombre es obligatorio");

        var validator2 = new InlineValidator<CreateEstablishmentCategoryCommand>();
        validator2.RuleFor(x => x.SortOrder).Must(_ => false).WithMessage("El orden es obligatorio");

        var behavior =
            new ValidationBehavior<CreateEstablishmentCategoryCommand, Result<EstablishmentCategoryId>>(
                [validator1, validator2]);

        MessageHandlerDelegate<CreateEstablishmentCategoryCommand, Result<EstablishmentCategoryId>> next =
            (_, _) => new ValueTask<Result<EstablishmentCategoryId>>(
                Result.Success(EstablishmentCategoryId.New()));

        var command = new CreateEstablishmentCategoryCommand("Nombre", null, null, 1);

        Result<EstablishmentCategoryId> result = await behavior.Handle(
            command, next, TestContext.Current.CancellationToken);

        Assert.Contains("El nombre es obligatorio", result.Error.Description);
        Assert.Contains("El orden es obligatorio", result.Error.Description);
        Assert.Contains("; ", result.Error.Description);
    }

    [Fact]
    public async Task Handle_ValidationFails_CancellationTokenPropagatedToValidators()
    {
        using var cts = new CancellationTokenSource();
        CancellationToken expectedToken = cts.Token;
        CancellationToken? capturedToken = null;

        var validator = new CapturingValidator(token => capturedToken = token);

        var behavior = new ValidationBehavior<DeactivateEstablishmentCategoryCommand, Result>(
            [validator]);

        MessageHandlerDelegate<DeactivateEstablishmentCategoryCommand, Result> next =
            (_, _) => new ValueTask<Result>(Result.Success());

        var command = new DeactivateEstablishmentCategoryCommand(Guid.NewGuid());

        await behavior.Handle(command, next, expectedToken);

        Assert.NotNull(capturedToken);
        Assert.Equal(expectedToken, capturedToken.Value);
    }

    [Fact]
    public async Task Handle_NoValidators_HandlerExecuted()
    {
        var behavior = new ValidationBehavior<DeactivateEstablishmentCategoryCommand, Result>(
            []);

        bool handlerCalled = false;
        MessageHandlerDelegate<DeactivateEstablishmentCategoryCommand, Result> next =
            (_, _) =>
            {
                handlerCalled = true;
                return new ValueTask<Result>(Result.Success());
            };

        var command = new DeactivateEstablishmentCategoryCommand(Guid.NewGuid());

        Result result = await behavior.Handle(
            command, next, TestContext.Current.CancellationToken);

        Assert.True(handlerCalled);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_AllValidatorsPass_HandlerExecuted()
    {
        var validator = new InlineValidator<DeactivateEstablishmentCategoryCommand>();
        validator.RuleFor(x => x.Id).Must(_ => true).WithMessage("No debería fallar");

        var behavior = new ValidationBehavior<DeactivateEstablishmentCategoryCommand, Result>(
            [validator]);

        bool handlerCalled = false;
        MessageHandlerDelegate<DeactivateEstablishmentCategoryCommand, Result> next =
            (_, _) =>
            {
                handlerCalled = true;
                return new ValueTask<Result>(Result.Success());
            };

        var command = new DeactivateEstablishmentCategoryCommand(Guid.NewGuid());

        Result result = await behavior.Handle(
            command, next, TestContext.Current.CancellationToken);

        Assert.True(handlerCalled);
        Assert.True(result.IsSuccess);
    }

    private sealed class CapturingValidator : AbstractValidator<DeactivateEstablishmentCategoryCommand>
    {
        private readonly Action<CancellationToken> _onValidate;

        public CapturingValidator(Action<CancellationToken> onValidate)
        {
            _onValidate = onValidate;
            RuleFor(x => x.Id).Must(_ => false).WithMessage("Fallo intencional");
        }

        public override Task<ValidationResult> ValidateAsync(
            ValidationContext<DeactivateEstablishmentCategoryCommand> context,
            CancellationToken cancellation = default)
        {
            _onValidate(cancellation);
            return base.ValidateAsync(context, cancellation);
        }
    }
}
