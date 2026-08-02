using FluentValidation;

namespace Lyria.Application.Features.UserRoles.Finalize;

public sealed class FinalizeUserRoleCommandValidator
    : AbstractValidator<FinalizeUserRoleCommand>
{
    public FinalizeUserRoleCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("El identificador del usuario es obligatorio.");

        RuleFor(x => x.UserRoleId)
            .NotEmpty()
            .WithMessage("El identificador de la asignación de rol es obligatorio.");
    }
}
