using FluentValidation;
using Lyria.Domain.Users;

namespace Lyria.Application.Features.EmailVerifications.Confirm;

/// <summary>
/// Validación de entrada de la confirmación.
/// </summary>
/// <remarks>
/// Comprueba únicamente la forma del código: seis dígitos. Un valor con otra longitud o
/// con caracteres no numéricos no puede corresponder a ningún código emitido, de modo
/// que rechazarlo aquí no revela nada sobre la cuenta.
/// </remarks>
public sealed class ConfirmEmailVerificationCommandValidator
    : AbstractValidator<ConfirmEmailVerificationCommand>
{
    /// <summary>
    /// Longitud exacta del código de verificación.
    /// </summary>
    public const int CodeLength = 6;

    public ConfirmEmailVerificationCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("El correo electrónico es obligatorio.")
            .MaximumLength(User.EmailMaxLength)
            .WithMessage(
                $"El correo electrónico no puede superar los {User.EmailMaxLength} caracteres.");

        RuleFor(x => x.Code)
            .NotEmpty()
            .WithMessage("El código de verificación es obligatorio.")
            .Must(code => code is not null &&
                          code.Length == CodeLength &&
                          code.All(char.IsAsciiDigit))
            .WithMessage($"El código de verificación debe tener {CodeLength} dígitos.");
    }
}
