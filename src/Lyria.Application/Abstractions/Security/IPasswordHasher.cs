namespace Lyria.Application.Abstractions.Security;

public interface IPasswordHasher
{
    /// <summary>
    /// Hash de una contraseña con el algoritmo vigente.
    /// </summary>
    string Hash(string password);

    /// <summary>
    /// Verifica una contraseña contra un hash almacenado.
    /// </summary>
    /// <remarks>
    /// La comparación la realiza el algoritmo de hashing en tiempo constante.
    /// Nunca se comparan hashes como cadenas ni se intenta revertir el hash.
    /// </remarks>
    PasswordVerificationOutcome Verify(string hashedPassword, string providedPassword);

    /// <summary>
    /// Hash válido que no corresponde a ninguna contraseña real.
    /// </summary>
    /// <remarks>
    /// Permite ejecutar una verificación de coste equivalente cuando el correo no
    /// existe, de modo que un atacante no pueda distinguir "usuario inexistente" de
    /// "contraseña incorrecta" midiendo el tiempo de respuesta.
    /// </remarks>
    string NonMatchingHash { get; }
}
