using FluentValidation;

namespace Lyria.Application.Features.UserRestrictions.Remove;

public sealed class RemoveRestrictionFromUserCommandValidator
    : AbstractValidator<RemoveRestrictionFromUserCommand>
{
    public RemoveRestrictionFromUserCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("El identificador del usuario es obligatorio.");

        RuleFor(x => x.RestrictionId)
            .NotEmpty().WithMessage("El identificador de la restricción es obligatorio.");
    }
}
