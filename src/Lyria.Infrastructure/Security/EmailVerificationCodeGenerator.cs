using System.Globalization;
using System.Security.Cryptography;
using Lyria.Application.Abstractions.Security;
using Microsoft.Extensions.Options;

namespace Lyria.Infrastructure.Security;

/// <summary>
/// Genera el código de verificación de seis dígitos.
/// </summary>
/// <remarks>
/// El valor procede de <see cref="RandomNumberGenerator.GetInt32(int, int)"/>, que usa el
/// generador criptográfico del sistema y descarta los valores que introducirían sesgo, de
/// modo que los 1 000 000 de códigos posibles son equiprobables.
///
/// Queda descartado cualquier origen predecible: <c>Random</c>, un GUID truncado, una
/// marca de tiempo o una secuencia. El formato <c>D6</c> conserva los ceros iniciales, de
/// forma que <c>000042</c> es un código tan válido como cualquier otro.
/// </remarks>
internal sealed class EmailVerificationCodeGenerator(IOptions<EmailVerificationOptions> options)
    : IEmailVerificationCodeGenerator
{
    /// <summary>
    /// Cota superior exclusiva del código: 999999 es el mayor valor posible.
    /// </summary>
    private const int ExclusiveUpperBound = 1_000_000;

    private readonly EmailVerificationOptions _options = options.Value;

    public string Generate()
    {
        if (_options.CodeLength != EmailVerificationOptions.SupportedCodeLength)
        {
            throw new InvalidOperationException(
                "La longitud del código de verificación configurada no está soportada.");
        }

        int value = RandomNumberGenerator.GetInt32(0, ExclusiveUpperBound);

        return value.ToString("D6", CultureInfo.InvariantCulture);
    }
}
