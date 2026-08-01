using FluentValidation;

namespace Lyria.Application.Features.Users.ChangePassword;

public sealed class ChangeUserPasswordCommandValidator
    : AbstractValidator<ChangeUserPasswordCommand>
{
    public ChangeUserPasswordCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("El identificador del usuario es obligatorio.");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("La contraseña es obligatoria.");
    }
}
