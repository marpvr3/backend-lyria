using FluentValidation;
using Lyria.Domain.Users;

namespace Lyria.Application.Features.Users.Update;

public sealed class UpdateUserCommandValidator
    : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("El identificador del usuario es obligatorio.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("El nombre es obligatorio.")
            .MinimumLength(User.NameMinLength)
            .WithMessage($"El nombre debe tener al menos {User.NameMinLength} caracteres.")
            .MaximumLength(User.NameMaxLength)
            .WithMessage($"El nombre no puede superar los {User.NameMaxLength} caracteres.");

        RuleFor(x => x.LastName)
            .NotEmpty()
            .WithMessage("El apellido es obligatorio.")
            .MinimumLength(User.LastNameMinLength)
            .WithMessage($"El apellido debe tener al menos {User.LastNameMinLength} caracteres.")
            .MaximumLength(User.LastNameMaxLength)
            .WithMessage($"El apellido no puede superar los {User.LastNameMaxLength} caracteres.");

        RuleFor(x => x.Phone)
            .MaximumLength(User.PhoneMaxLength)
            .WithMessage($"El teléfono no puede superar los {User.PhoneMaxLength} caracteres.")
            .When(x => x.Phone is not null);

        RuleFor(x => x.PhotoUrl)
            .MaximumLength(User.PhotoUrlMaxLength)
            .WithMessage($"La URL de la foto no puede superar los {User.PhotoUrlMaxLength} caracteres.")
            .When(x => x.PhotoUrl is not null);
    }
}
