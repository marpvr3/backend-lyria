using FluentValidation;
using Lyria.Domain.Users;

namespace Lyria.Application.Features.EmailVerifications.Resend;

/// <summary>
/// Validación de entrada del reenvío.
/// </summary>
/// <remarks>
/// Deliberadamente mínima, por la misma razón que la del inicio de sesión: solo
/// comprueba presencia y longitud máxima. No consulta la base de datos ni exige un
/// formato estricto, porque distinguir entradas produciría respuestas distinguibles.
/// </remarks>
public sealed class ResendEmailVerificationCommandValidator
    : AbstractValidator<ResendEmailVerificationCommand>
{
    public ResendEmailVerificationCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("El correo electrónico es obligatorio.")
            .MaximumLength(User.EmailMaxLength)
            .WithMessage(
                $"El correo electrónico no puede superar los {User.EmailMaxLength} caracteres.");
    }
}
