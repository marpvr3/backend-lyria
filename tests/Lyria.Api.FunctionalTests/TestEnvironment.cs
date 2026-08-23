using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Lyria.Api.FunctionalTests;

/// <summary>
/// Prepara las variables de entorno que necesita el host de pruebas.
/// </summary>
/// <remarks>
/// La configuración JWT y la de verificación de correo se validan al arrancar
/// (<c>ValidateOnStart</c>), de modo que ningún host puede iniciarse sin clave de firma
/// ni sin secreto de códigos. Aquí se genera un valor criptográficamente aleatorio para
/// cada uno, por ejecución del proceso de pruebas.
///
/// Deliberadamente no hay ningún secreto fijo en el repositorio: un valor compartido y
/// versionado sería un secreto filtrado, y las pruebas no necesitan que sea estable entre
/// ejecuciones. Los dos secretos son independientes entre sí, igual que en producción.
///
/// Se usan las variables de entorno <c>Jwt__SigningKey</c> y
/// <c>EmailVerification__CodeSecret</c> —el mismo mecanismo que en producción— para que
/// todas las pruebas funcionales las hereden sin configurarlas una por una.
///
/// No se configura ningún dato SMTP: las pruebas funcionales sustituyen el remitente por
/// un doble y nunca envían correo real.
/// </remarks>
internal static class TestEnvironment
{
    [ModuleInitializer]
    public static void Initialize()
    {
        Environment.SetEnvironmentVariable(
            "Jwt__SigningKey",
            Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)));

        Environment.SetEnvironmentVariable(
            "EmailVerification__CodeSecret",
            Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)));
    }
}
