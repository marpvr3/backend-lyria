using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Lyria.Api.FunctionalTests;

/// <summary>
/// Prepara las variables de entorno que necesita el host de pruebas.
/// </summary>
/// <remarks>
/// La configuración JWT se valida al arrancar (<c>ValidateOnStart</c>), de modo que
/// ningún host puede iniciarse sin clave de firma. Aquí se genera una clave
/// criptográficamente aleatoria por ejecución del proceso de pruebas.
///
/// Deliberadamente no hay ninguna clave fija en el repositorio: una clave compartida
/// y versionada sería un secreto filtrado, y las pruebas no necesitan que el valor sea
/// estable entre ejecuciones.
///
/// Se usa la variable de entorno <c>Jwt__SigningKey</c> —el mismo mecanismo que en
/// producción— para que todas las pruebas funcionales la hereden sin tener que
/// configurarla una por una.
/// </remarks>
internal static class TestEnvironment
{
    [ModuleInitializer]
    public static void Initialize()
    {
        Environment.SetEnvironmentVariable(
            "Jwt__SigningKey",
            Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)));
    }
}
