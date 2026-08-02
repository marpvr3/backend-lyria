using FluentValidation;

namespace Lyria.Application.Features.UserRoles.SetActiveStatus;

public sealed class SetUserRoleActiveStatusCommandValidator
    : AbstractValidator<SetUserRoleActiveStatusCommand>
{
    public SetUserRoleActiveStatusCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("El identificador del usuario es obligatorio.");

        RuleFor(x => x.UserRoleId)
            .NotEmpty()
            .WithMessage("El identificador de la asignación de rol es obligatorio.");
    }
}
