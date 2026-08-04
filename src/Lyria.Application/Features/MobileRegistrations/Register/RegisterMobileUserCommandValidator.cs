using FluentValidation;
using Lyria.Domain.Users;

namespace Lyria.Application.Features.MobileRegistrations.Register;

/// <summary>
/// Validación de entrada del registro móvil.
/// </summary>
public sealed class RegisterMobileUserCommandValidator
    : AbstractValidator<RegisterMobileUserCommand>
{
    /// <summary>
    /// Longitud mínima exigida a la contraseña en el registro móvil.
    /// </summary>
    public const int PasswordMinLength = 8;

    /// <summary>
    /// Longitud máxima admitida para la contraseña en el registro móvil.
    /// </summary>
    public const int PasswordMaxLength = 128;

    public RegisterMobileUserCommandValidator()
    {
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

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("El correo electrónico es obligatorio.")
            .MaximumLength(User.EmailMaxLength)
            .WithMessage($"El correo electrónico no puede superar los {User.EmailMaxLength} caracteres.");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("La contraseña es obligatoria.")
            .MinimumLength(PasswordMinLength)
            .WithMessage($"La contraseña debe tener al menos {PasswordMinLength} caracteres.")
            .MaximumLength(PasswordMaxLength)
            .WithMessage($"La contraseña no puede superar los {PasswordMaxLength} caracteres.");

        RuleFor(x => x.Phone)
            .MaximumLength(User.PhoneMaxLength)
            .WithMessage($"El teléfono no puede superar los {User.PhoneMaxLength} caracteres.")
            .When(x => x.Phone is not null);

        RuleFor(x => x.PhotoUrl)
            .MaximumLength(User.PhotoUrlMaxLength)
            .WithMessage($"La URL de la foto no puede superar los {User.PhotoUrlMaxLength} caracteres.")
            .When(x => x.PhotoUrl is not null);

        RuleFor(x => x.RestrictionIds)
            .Must(ids => ids.All(id => id != Guid.Empty))
            .WithMessage("Los identificadores de restricción no pueden estar vacíos.")
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("No se permiten identificadores de restricción duplicados.")
            .When(x => x.RestrictionIds is not null);
    }
}
