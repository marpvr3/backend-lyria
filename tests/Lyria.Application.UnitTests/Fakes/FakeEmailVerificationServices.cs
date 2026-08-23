using System.Globalization;
using Lyria.Application.Abstractions.Notifications;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Abstractions.Security;
using Lyria.Application.Abstractions.Services;
using Lyria.Domain.Users;
using Lyria.Domain.Users.EmailVerifications;

namespace Lyria.Application.UnitTests.Fakes;

/// <summary>
/// Generador de códigos determinista. Conserva la propiedad esencial: seis dígitos
/// que pueden empezar por cero.
/// </summary>
internal sealed class FakeEmailVerificationCodeGenerator : IEmailVerificationCodeGenerator
{
    private int _counter;

    /// <summary>
    /// Cuando se establece, se devuelve siempre este código.
    /// </summary>
    public string? FixedCode { get; set; }

    public List<string> GeneratedCodes { get; } = [];

    public string Generate()
    {
        _counter++;

        string code = FixedCode ?? _counter.ToString("D6", CultureInfo.InvariantCulture);

        GeneratedCodes.Add(code);

        return code;
    }
}

/// <summary>
/// Hasher determinista. Emula la relación código → hash sin criptografía, conservando
/// lo esencial: el hash no es el código y tiene el formato que exige el dominio.
/// </summary>
internal sealed class FakeEmailVerificationCodeHasher : IEmailVerificationCodeHasher
{
    public string ComputeHash(string code)
    {
        // Suma simple y estable entre ejecuciones: no se usa GetHashCode porque está
        // aleatorizado por proceso.
        uint value = 2166136261u;

        foreach (char character in code)
        {
            value = (value ^ character) * 16777619u;
        }

        string seed = value.ToString("x8", CultureInfo.InvariantCulture);

        return string.Concat(Enumerable.Repeat(seed, 8));
    }

    public bool Matches(string codeHash, string code) =>
        string.Equals(codeHash, ComputeHash(code), StringComparison.Ordinal);
}

/// <summary>
/// Remitente de correo simulado. Nunca envía nada real.
/// </summary>
internal sealed class FakeEmailSender : IEmailSender
{
    /// <summary>
    /// Cuando se establece, el envío falla. Simula un problema del proveedor SMTP.
    /// </summary>
    public Exception? FailureToThrow { get; set; }

    /// <summary>
    /// Se ejecuta al enviar, antes de fallar. Permite observar el estado del sistema en
    /// el instante exacto del envío.
    /// </summary>
    public Action? OnSend { get; set; }

    public List<SentEmail> SentEmails { get; } = [];

    public Task SendEmailVerificationCodeAsync(
        string recipientEmail,
        string recipientName,
        string verificationCode,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken)
    {
        OnSend?.Invoke();

        if (FailureToThrow is not null)
        {
            return Task.FromException(FailureToThrow);
        }

        SentEmails.Add(new SentEmail(
            recipientEmail, recipientName, verificationCode, expiresAtUtc));

        return Task.CompletedTask;
    }

    internal sealed record SentEmail(
        string RecipientEmail,
        string RecipientName,
        string VerificationCode,
        DateTime ExpiresAtUtc);
}

/// <summary>
/// Valores de política de la verificación de correo.
/// </summary>
internal sealed class FakeEmailVerificationDefaults : IEmailVerificationDefaults
{
    public int ExpirationMinutes { get; set; } = 15;

    public int MaximumFailedAttempts { get; set; } = 5;

    public int ResendCooldownSeconds { get; set; } = 60;
}

/// <summary>
/// Repositorio de verificaciones en memoria.
/// </summary>
internal sealed class FakeUserEmailVerificationRepository : IUserEmailVerificationRepository
{
    private readonly List<UserEmailVerification> _verifications = [];

    public IReadOnlyList<UserEmailVerification> Verifications => _verifications;

    public void Seed(UserEmailVerification verification) => _verifications.Add(verification);

    public Task<UserEmailVerification?> GetLatestPendingAsync(
        UserId userId,
        CancellationToken cancellationToken) =>
        Task.FromResult(Pending(userId).FirstOrDefault());

    public Task<IReadOnlyList<UserEmailVerification>> GetPendingAsync(
        UserId userId,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<UserEmailVerification>>([.. Pending(userId)]);

    public Task<DateTime?> GetLastCreatedAtUtcAsync(
        UserId userId,
        CancellationToken cancellationToken)
    {
        DateTime? latest = _verifications
            .Where(v => v.UserId == userId)
            .OrderByDescending(v => v.CreatedAtUtc)
            .Select(v => (DateTime?)v.CreatedAtUtc)
            .FirstOrDefault();

        return Task.FromResult(latest);
    }

    private IEnumerable<UserEmailVerification> Pending(UserId userId) =>
        _verifications
            .Where(v => v.UserId == userId && !v.IsUsed && !v.IsRevoked)
            .OrderByDescending(v => v.CreatedAtUtc);
}

/// <summary>
/// Escritor transaccional simulado de la verificación de correo.
/// </summary>
internal sealed class FakeEmailVerificationWriter : IEmailVerificationWriter
{
    /// <summary>
    /// Cuando se establece, la operación falla en lugar de confirmar.
    /// </summary>
    public Exception? FailureToThrow { get; set; }

    /// <summary>
    /// Cuando es <c>true</c>, la confirmación se comporta como si otra solicitud
    /// simultánea hubiera canjeado el código primero.
    /// </summary>
    public bool SimulateConcurrentConfirmation { get; set; }

    public bool ResendCommitted { get; private set; }
    public bool ConfirmationCommitted { get; private set; }
    public bool FailedAttemptCommitted { get; private set; }

    public IReadOnlyCollection<UserEmailVerification> RevokedVerifications { get; private set; } = [];
    public UserEmailVerification? CreatedVerification { get; private set; }
    public User? ConfirmedUser { get; private set; }
    public UserEmailVerification? ConfirmedVerification { get; private set; }
    public UserEmailVerification? FailedVerification { get; private set; }

    public Task ResendAsync(
        IReadOnlyCollection<UserEmailVerification> revokedVerifications,
        UserEmailVerification createdVerification,
        CancellationToken cancellationToken)
    {
        if (FailureToThrow is not null)
        {
            return Task.FromException(FailureToThrow);
        }

        RevokedVerifications = revokedVerifications;
        CreatedVerification = createdVerification;
        ResendCommitted = true;

        return Task.CompletedTask;
    }

    public Task<bool> TryConfirmAsync(
        User user,
        UserEmailVerification verification,
        CancellationToken cancellationToken)
    {
        if (FailureToThrow is not null)
        {
            return Task.FromException<bool>(FailureToThrow);
        }

        if (SimulateConcurrentConfirmation)
        {
            return Task.FromResult(false);
        }

        ConfirmedUser = user;
        ConfirmedVerification = verification;
        ConfirmationCommitted = true;

        return Task.FromResult(true);
    }

    public Task RegisterFailedAttemptAsync(
        UserEmailVerification verification,
        CancellationToken cancellationToken)
    {
        if (FailureToThrow is not null)
        {
            return Task.FromException(FailureToThrow);
        }

        FailedVerification = verification;
        FailedAttemptCommitted = true;

        return Task.CompletedTask;
    }
}
