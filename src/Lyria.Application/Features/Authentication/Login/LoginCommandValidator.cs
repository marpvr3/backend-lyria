using FluentValidation;
using Lyria.Domain.Users;

namespace Lyria.Application.Features.Authentication.Login;

/// <summary>
/// Validación de entrada del inicio de sesión.
/// </summary>
/// <remarks>
/// Deliberadamente mínima: solo comprueba presencia y longitud máxima. No exige
/// formato de correo ni longitud mínima de contraseña, porque un rechazo de validación
/// (400) frente a un rechazo de credenciales (401) daría al atacante una señal
/// distinguible, y porque una cuenta antigua podría tener una contraseña más corta
/// que la exigida hoy en el registro.
/// </remarks>
public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    /// <summary>
    /// Longitud máxima admitida para la contraseña recibida.
    /// </summary>
    public const int PasswordMaxLength = 128;

    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("El correo electrónico es obligatorio.")
            .MaximumLength(User.EmailMaxLength)
            .WithMessage($"El correo electrónico no puede superar los {User.EmailMaxLength} caracteres.");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("La contraseña es obligatoria.")
            .MaximumLength(PasswordMaxLength)
            .WithMessage($"La contraseña no puede superar los {PasswordMaxLength} caracteres.");
    }
}
