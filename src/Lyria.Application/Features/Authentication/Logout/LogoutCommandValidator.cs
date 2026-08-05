using FluentValidation;
using Lyria.Application.Features.Authentication.Refresh;

namespace Lyria.Application.Features.Authentication.Logout;

/// <summary>
/// Validación de entrada del cierre de sesión.
/// </summary>
/// <remarks>
/// Solo comprueba presencia y longitud máxima. Que el token exista o no es
/// irrelevante: el cierre de sesión es idempotente y nunca revela esa información.
/// </remarks>
public sealed class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .WithMessage("El refresh token es obligatorio.")
            .MaximumLength(RefreshAuthenticationCommandValidator.RefreshTokenMaxLength)
            .WithMessage(
                "El refresh token no puede superar los " +
                $"{RefreshAuthenticationCommandValidator.RefreshTokenMaxLength} caracteres.");
    }
}
