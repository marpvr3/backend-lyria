using System.Security.Cryptography;
using System.Text;
using Lyria.Application.Abstractions.Security;
using Lyria.Domain.Users.EmailVerifications;
using Microsoft.Extensions.Options;

namespace Lyria.Infrastructure.Security;

/// <summary>
/// Calcula y verifica el hash con el que se almacenan los códigos de verificación.
/// </summary>
/// <remarks>
/// Se usa <b>HMAC-SHA256 con un secreto del servidor</b>, no un SHA-256 simple. La
/// diferencia es esencial: el espacio de seis dígitos tiene un millón de valores y puede
/// recorrerse por completo en un instante, de modo que un digest sin clave permitiría
/// recuperar todos los códigos si la base de datos se filtrara. Con HMAC, quien obtenga
/// las filas necesita además el secreto, que vive fuera de la base de datos y solo se
/// suministra por variable de entorno.
///
/// El secreto es exclusivo de este mecanismo y nunca es la clave de firma de los JWT.
///
/// El hash se almacena en hexadecimal de 64 caracteres y la comparación es de tiempo
/// constante. Ni el código ni el hash ni el secreto se registran en ningún log.
/// </remarks>
internal sealed class EmailVerificationCodeHasher : IEmailVerificationCodeHasher
{
    private readonly byte[] _secret;

    public EmailVerificationCodeHasher(IOptions<EmailVerificationOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _secret = Encoding.UTF8.GetBytes(options.Value.CodeSecret);
    }

    public string ComputeHash(string code)
    {
        ArgumentNullException.ThrowIfNull(code);

        byte[] hash = HMACSHA256.HashData(_secret, Encoding.UTF8.GetBytes(code));

        return Convert.ToHexStringLower(hash);
    }

    public bool Matches(string codeHash, string code)
    {
        if (codeHash is null || code is null)
        {
            return false;
        }

        string expected = UserEmailVerification.NormalizeCodeHash(codeHash);
        string actual = ComputeHash(code);

        // FixedTimeEquals no cortocircuita ante longitudes distintas ni ante el primer
        // byte discrepante: el tiempo de respuesta no informa de cuánto se acertó.
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(actual),
            Encoding.UTF8.GetBytes(expected));
    }
}
