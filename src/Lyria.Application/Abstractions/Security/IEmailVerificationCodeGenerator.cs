namespace Lyria.Application.Abstractions.Security;

/// <summary>
/// Genera el código numérico con el que el usuario confirma su correo electrónico.
/// </summary>
/// <remarks>
/// La implementación vive en Infrastructure y debe usar una fuente criptográficamente
/// segura. Quedan explícitamente descartados <c>Random</c>, los GUID truncados, las
/// marcas de tiempo y cualquier secuencia predecible.
/// </remarks>
public interface IEmailVerificationCodeGenerator
{
    /// <summary>
    /// Genera un código de seis dígitos entre <c>000000</c> y <c>999999</c>.
    /// El cero inicial es significativo y se conserva.
    /// </summary>
    string Generate();
}
