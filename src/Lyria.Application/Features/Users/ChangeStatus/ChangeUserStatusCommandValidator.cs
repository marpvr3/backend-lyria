using FluentValidation;

namespace Lyria.Application.Features.Users.ChangeStatus;

public sealed class ChangeUserStatusCommandValidator
    : AbstractValidator<ChangeUserStatusCommand>
{
    public ChangeUserStatusCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("El identificador del usuario es obligatorio.");

        RuleFor(x => x.Status)
            .NotEmpty()
            .WithMessage("El estado es obligatorio.");
    }
}
