using Lyria.Application.Abstractions.Security;

namespace Lyria.Application.UnitTests.Fakes;

internal sealed class FakePasswordHasher : IPasswordHasher
{
    /// <summary>
    /// Hash señuelo devuelto cuando el usuario no existe. Ninguna contraseña lo produce.
    /// </summary>
    public const string NonMatchingHashValue = "hash-que-nunca-coincide";

    /// <summary>
    /// Resultado que devuelve <see cref="Verify"/> cuando la contraseña sí corresponde
    /// al hash almacenado. Permite ejercitar el camino de rehash.
    /// </summary>
    public PasswordVerificationOutcome SuccessOutcome { get; set; } =
        PasswordVerificationOutcome.Success;

    /// <summary>
    /// Hashes contra los que se invocó la verificación, en orden.
    /// </summary>
    public List<string> VerifiedHashes { get; } = [];

    /// <summary>
    /// Sufijo que se añade a los hashes generados. Al cambiarlo entre la creación del
    /// usuario y el inicio de sesión, un rehash produce un valor distinguible del
    /// original y puede comprobarse que realmente se aplicó.
    /// </summary>
    public string HashSuffix { get; set; } = string.Empty;

    /// <summary>
    /// Número de veces que se invocó <see cref="Hash"/>. La verificación no lo altera.
    /// </summary>
    public int HashCallCount { get; private set; }

    public string NonMatchingHash => NonMatchingHashValue;

    public string Hash(string password)
    {
        HashCallCount++;

        return ComputeHash(password);
    }

    public PasswordVerificationOutcome Verify(string hashedPassword, string providedPassword)
    {
        VerifiedHashes.Add(hashedPassword);

        // La verificación ignora el sufijo vigente: un hash generado con un sufijo
        // anterior sigue correspondiendo a la contraseña, que es justo el escenario
        // que motiva un rehash.
        bool matches = hashedPassword.StartsWith(
            "hashed_" + providedPassword, StringComparison.Ordinal);

        return matches ? SuccessOutcome : PasswordVerificationOutcome.Failed;
    }

    private string ComputeHash(string password) => "hashed_" + password + HashSuffix;
}
