using System.Security.Cryptography;
using Lyria.Application.Abstractions.Security;
using Microsoft.AspNetCore.Identity;

namespace Lyria.Infrastructure.Security;

/// <summary>
/// Hash y verificación de contraseñas mediante el algoritmo de ASP.NET Core Identity
/// (PBKDF2 con sal por contraseña).
/// </summary>
/// <remarks>
/// Identity queda confinado en Infrastructure: Application solo conoce
/// <see cref="IPasswordHasher"/> y <see cref="PasswordVerificationOutcome"/>.
/// La verificación la realiza íntegramente la biblioteca, en tiempo constante y sin
/// comparar hashes como cadenas.
/// </remarks>
internal sealed class PasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<object> _hasher = new();

    private readonly Lazy<string> _nonMatchingHash;

    public PasswordHasher()
    {
        // Se calcula una sola vez y de forma diferida sobre un valor aleatorio: es un
        // hash real, con el mismo coste de verificación que cualquier otro, pero cuya
        // contraseña de origen nadie conoce ni puede adivinar.
        _nonMatchingHash = new Lazy<string>(() =>
            _hasher.HashPassword(
                null!,
                Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32))));
    }

    public string NonMatchingHash => _nonMatchingHash.Value;

    public string Hash(string password)
    {
        return _hasher.HashPassword(null!, password);
    }

    public PasswordVerificationOutcome Verify(string hashedPassword, string providedPassword)
    {
        PasswordVerificationResult result;

        try
        {
            result = _hasher.VerifyHashedPassword(null!, hashedPassword, providedPassword);
        }
        catch (FormatException)
        {
            // Un hash almacenado con un formato irreconocible no puede verificarse.
            // Se trata como credencial inválida en lugar de propagar un error 500, que
            // además revelaría al cliente que la cuenta existe.
            return PasswordVerificationOutcome.Failed;
        }

        return result switch
        {
            PasswordVerificationResult.Success =>
                PasswordVerificationOutcome.Success,
            PasswordVerificationResult.SuccessRehashNeeded =>
                PasswordVerificationOutcome.SuccessRehashNeeded,
            _ => PasswordVerificationOutcome.Failed
        };
    }
}
