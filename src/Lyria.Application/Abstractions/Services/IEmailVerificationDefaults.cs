namespace Lyria.Application.Abstractions.Services;

/// <summary>
/// Parámetros de política de la verificación de correo que los casos de uso necesitan.
/// La implementación se configura en el composition root.
/// </summary>
/// <remarks>
/// Expone únicamente valores de política. El secreto con el que se hashean los códigos
/// (<c>EmailVerification:CodeSecret</c>) <b>no</b> forma parte de este contrato y jamás
/// llega a la capa Application: solo lo conoce el componente de Infrastructure que
/// calcula el hash.
/// </remarks>
public interface IEmailVerificationDefaults
{
    /// <summary>
    /// Minutos de vigencia de un código de verificación.
    /// </summary>
    int ExpirationMinutes { get; }

    /// <summary>
    /// Intentos fallidos que invalidan un código.
    /// </summary>
    int MaximumFailedAttempts { get; }

    /// <summary>
    /// Segundos que deben transcurrir entre dos envíos al mismo usuario.
    /// </summary>
    int ResendCooldownSeconds { get; }
}
