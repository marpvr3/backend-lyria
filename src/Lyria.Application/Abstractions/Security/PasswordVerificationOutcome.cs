namespace Lyria.Application.Abstractions.Security;

/// <summary>
/// Resultado de verificar una contraseña contra su hash almacenado.
/// </summary>
/// <remarks>
/// Es un contrato propio de Application: evita que la capa de aplicación dependa de
/// ASP.NET Core Identity, cuyo <c>PasswordVerificationResult</c> permanece confinado
/// en Infrastructure.
/// </remarks>
public enum PasswordVerificationOutcome
{
    /// <summary>
    /// La contraseña no corresponde al hash almacenado.
    /// </summary>
    Failed = 0,

    /// <summary>
    /// La contraseña es correcta y el hash usa el algoritmo y los parámetros vigentes.
    /// </summary>
    Success = 1,

    /// <summary>
    /// La contraseña es correcta pero el hash quedó obsoleto y debe regenerarse
    /// con el algoritmo actual.
    /// </summary>
    SuccessRehashNeeded = 2
}
