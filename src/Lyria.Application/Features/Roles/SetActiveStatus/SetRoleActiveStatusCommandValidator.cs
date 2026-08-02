using FluentValidation;

namespace Lyria.Application.Features.Roles.SetActiveStatus;

public sealed class SetRoleActiveStatusCommandValidator
    : AbstractValidator<SetRoleActiveStatusCommand>
{
    public SetRoleActiveStatusCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("El identificador es obligatorio.");
    }
}
