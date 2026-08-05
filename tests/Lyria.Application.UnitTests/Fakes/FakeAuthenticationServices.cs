using System.Globalization;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Abstractions.Security;
using Lyria.Domain.Users;
using Lyria.Domain.Users.RefreshTokens;

namespace Lyria.Application.UnitTests.Fakes;

/// <summary>
/// Emisor de access tokens determinista.
/// </summary>
internal sealed class FakeAccessTokenService : IAccessTokenService
{
    public DateTime ExpiresAtUtc { get; set; } =
        new(2026, 8, 4, 23, 15, 0, DateTimeKind.Utc);

    public List<UserId> IssuedFor { get; } = [];

    public AccessToken Issue(UserId userId)
    {
        IssuedFor.Add(userId);

        return new AccessToken($"access-token-{userId.Value}", ExpiresAtUtc);
    }
}

/// <summary>
/// Generador de refresh tokens determinista. Emula la relación token → hash sin
/// criptografía, conservando la propiedad esencial: el hash no es el token.
/// </summary>
internal sealed class FakeRefreshTokenGenerator : IRefreshTokenGenerator
{
    private int _counter;

    public TimeSpan Lifetime { get; set; } = TimeSpan.FromDays(30);

    public List<string> GeneratedTokens { get; } = [];

    public GeneratedRefreshToken Generate()
    {
        _counter++;

        string token = $"token-opaco-{_counter.ToString(CultureInfo.InvariantCulture)}";

        GeneratedTokens.Add(token);

        return new GeneratedRefreshToken(token, ComputeHash(token));
    }

    /// <summary>
    /// Hash simulado: 64 caracteres hexadecimales derivados del token, como exige
    /// <see cref="UserRefreshToken"/>.
    /// </summary>
    public string ComputeHash(string refreshToken)
    {
        // Suma simple y estable entre ejecuciones: no se usa GetHashCode porque está
        // aleatorizado por proceso.
        uint code = 2166136261u;

        foreach (char character in refreshToken)
        {
            code = (code ^ character) * 16777619u;
        }

        string seed = code.ToString("x8", CultureInfo.InvariantCulture);

        return string.Concat(Enumerable.Repeat(seed, 8));
    }
}

/// <summary>
/// Repositorio de sesiones en memoria.
/// </summary>
internal sealed class FakeUserRefreshTokenRepository : IUserRefreshTokenRepository
{
    private readonly List<UserRefreshToken> _sessions = [];

    public IReadOnlyList<UserRefreshToken> Sessions => _sessions;

    public void Seed(UserRefreshToken session) => _sessions.Add(session);

    public Task<UserRefreshToken?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken)
    {
        string normalized = UserRefreshToken.NormalizeTokenHash(tokenHash);

        UserRefreshToken? found = _sessions.FirstOrDefault(
            s => string.Equals(s.TokenHash, normalized, StringComparison.Ordinal));

        return Task.FromResult(found);
    }
}

/// <summary>
/// Escritor transaccional simulado.
/// </summary>
/// <remarks>
/// Registra lo confirmado y permite forzar un fallo para comprobar que el caso de uso
/// no da por buena una escritura que no se confirmó.
/// </remarks>
internal sealed class FakeAuthenticationSessionWriter : IAuthenticationSessionWriter
{
    /// <summary>
    /// Cuando se establece, la operación falla en lugar de confirmar.
    /// </summary>
    public Exception? FailureToThrow { get; set; }

    public bool LoginCommitted { get; private set; }
    public bool RotationCommitted { get; private set; }
    public bool RevocationCommitted { get; private set; }

    public User? CommittedUser { get; private set; }
    public UserRefreshToken? CommittedSession { get; private set; }
    public UserRefreshToken? RevokedSession { get; private set; }
    public UserRefreshToken? CreatedSession { get; private set; }

    public Task CompleteLoginAsync(
        User user,
        UserRefreshToken session,
        CancellationToken cancellationToken)
    {
        if (FailureToThrow is not null)
        {
            return Task.FromException(FailureToThrow);
        }

        LoginCommitted = true;
        CommittedUser = user;
        CommittedSession = session;

        return Task.CompletedTask;
    }

    public Task RotateAsync(
        UserRefreshToken revokedSession,
        UserRefreshToken createdSession,
        CancellationToken cancellationToken)
    {
        if (FailureToThrow is not null)
        {
            return Task.FromException(FailureToThrow);
        }

        RotationCommitted = true;
        RevokedSession = revokedSession;
        CreatedSession = createdSession;

        return Task.CompletedTask;
    }

    public Task RevokeAsync(
        UserRefreshToken revokedSession,
        CancellationToken cancellationToken)
    {
        if (FailureToThrow is not null)
        {
            return Task.FromException(FailureToThrow);
        }

        RevocationCommitted = true;
        RevokedSession = revokedSession;

        return Task.CompletedTask;
    }
}

/// <summary>
/// Identidad autenticada simulada.
/// </summary>
internal sealed class FakeCurrentUser : ICurrentUser
{
    public UserId? UserId { get; set; }
}
