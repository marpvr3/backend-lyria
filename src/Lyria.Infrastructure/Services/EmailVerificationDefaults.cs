using Lyria.Application.Abstractions.Services;
using Lyria.Infrastructure.Security;
using Microsoft.Extensions.Options;

namespace Lyria.Infrastructure.Services;

/// <summary>
/// Expone a los casos de uso los valores de política de la verificación de correo.
/// </summary>
/// <remarks>
/// Deliberadamente no expone <see cref="EmailVerificationOptions.CodeSecret"/>: el
/// secreto solo lo conoce <see cref="EmailVerificationCodeHasher"/>.
/// </remarks>
internal sealed class EmailVerificationDefaults(IOptions<EmailVerificationOptions> options)
    : IEmailVerificationDefaults
{
    public int ExpirationMinutes => options.Value.ExpirationMinutes;

    public int MaximumFailedAttempts => options.Value.MaximumFailedAttempts;

    public int ResendCooldownSeconds => options.Value.ResendCooldownSeconds;
}
