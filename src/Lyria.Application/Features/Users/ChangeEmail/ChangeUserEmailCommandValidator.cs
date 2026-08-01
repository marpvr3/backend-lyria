using FluentValidation;
using Lyria.Domain.Users;

namespace Lyria.Application.Features.Users.ChangeEmail;

public sealed class ChangeUserEmailCommandValidator
    : AbstractValidator<ChangeUserEmailCommand>
{
    public ChangeUserEmailCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("El identificador del usuario es obligatorio.");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("El correo electrónico es obligatorio.")
            .MaximumLength(User.EmailMaxLength)
            .WithMessage($"El correo electrónico no puede superar los {User.EmailMaxLength} caracteres.");
    }
}
