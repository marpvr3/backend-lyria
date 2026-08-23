namespace Lyria.Application.Abstractions.Security;

/// <summary>
/// Calcula y verifica el hash con el que se almacena un código de verificación.
/// </summary>
/// <remarks>
/// El espacio de seis dígitos es lo bastante pequeño para recorrerse por completo, de
/// modo que un digest sin clave no protegería los códigos si la base de datos se
/// filtrara. La implementación usa un MAC con un secreto del servidor que nunca se
/// almacena junto a los datos.
/// </remarks>
public interface IEmailVerificationCodeHasher
{
    /// <summary>
    /// Calcula el hash del código en hexadecimal de 64 caracteres.
    /// </summary>
    string ComputeHash(string code);

    /// <summary>
    /// Comprueba, en tiempo constante, si el código corresponde al hash almacenado.
    /// </summary>
    bool Matches(string codeHash, string code);
}
