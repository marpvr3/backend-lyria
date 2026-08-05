using FluentValidation;

namespace Lyria.Application.Features.Authentication.Refresh;

/// <summary>
/// Validación de entrada de la renovación de tokens.
/// </summary>
/// <remarks>
/// Solo comprueba presencia y longitud máxima. La validez real del token se resuelve
/// en el caso de uso y siempre con la misma respuesta genérica.
/// </remarks>
public sealed class RefreshAuthenticationCommandValidator
    : AbstractValidator<RefreshAuthenticationCommand>
{
    /// <summary>
    /// Longitud máxima admitida para el refresh token recibido.
    /// </summary>
    public const int RefreshTokenMaxLength = 256;

    public RefreshAuthenticationCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .WithMessage("El refresh token es obligatorio.")
            .MaximumLength(RefreshTokenMaxLength)
            .WithMessage($"El refresh token no puede superar los {RefreshTokenMaxLength} caracteres.");
    }
}
