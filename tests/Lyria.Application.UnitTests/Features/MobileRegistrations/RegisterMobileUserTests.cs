using System.Reflection;
using System.Text.Json;
using Lyria.Application.Common.Errors;
using Lyria.Application.Common.Results;
using Lyria.Application.Features.MobileRegistrations;
using Lyria.Application.Features.MobileRegistrations.Register;
using Lyria.Application.UnitTests.Fakes;
using Lyria.Domain.Restrictions;
using Lyria.Domain.Roles;
using Lyria.Domain.Users;
using Lyria.Domain.Users.EmailVerifications;
using Lyria.Domain.Users.UserRestrictions;
using Lyria.Domain.Users.UserRoles;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Lyria.Application.UnitTests.Features.MobileRegistrations;

public sealed class RegisterMobileUserTests
{
    private static readonly DateTimeOffset FixedNow =
        new(2026, 8, 3, 10, 30, 0, TimeSpan.Zero);

    private const string PlainPassword = "Password123";

    private readonly FakeUserRepository _userRepository = new();
    private readonly FakeRoleRepository _roleRepository = new();
    private readonly FakeRestrictionRepository _restrictionRepository = new();
    private readonly FakeMobileRegistrationWriter _writer = new();
    private readonly FakeMobileRegistrationDefaults _defaults = new();
    private readonly FakeEmailVerificationDefaults _verificationDefaults = new();
    private readonly FakePasswordHasher _passwordHasher = new();
    private readonly FakeEmailVerificationCodeGenerator _codeGenerator = new();
    private readonly FakeEmailVerificationCodeHasher _codeHasher = new();
    private readonly FakeEmailSender _emailSender = new();
    private readonly FixedTimeProvider _timeProvider = new(FixedNow);

    private readonly RoleId _roleId = RoleId.New();
    private readonly Role _role;

    public RegisterMobileUserTests()
    {
        // Escenario base: el rol configurado existe y está activo.
        _role = Role.Create(_roleId, "Usuario", null);
        _roleRepository.Seed(_role);
        _defaults.DefaultRoleId = _roleId.Value.ToString();
    }

    private RegisterMobileUserCommandHandler CreateHandler() =>
        new(_userRepository,
            _roleRepository,
            _restrictionRepository,
            _writer,
            _defaults,
            _verificationDefaults,
            _passwordHasher,
            _codeGenerator,
            _codeHasher,
            _emailSender,
            _timeProvider,
            NullLogger<RegisterMobileUserCommandHandler>.Instance);

    private RestrictionId SeedRestriction(bool isActive = true)
    {
        var restriction = Restriction.Create(RestrictionId.New(), $"R-{Guid.NewGuid():N}", null);

        if (!isActive)
        {
            restriction.Deactivate();
        }

        _restrictionRepository.Seed(restriction);
        return restriction.Id;
    }

    private static RegisterMobileUserCommand CommandWith(params Guid[] restrictionIds) =>
        new("Andres", "Perez", "andres@email.com", PlainPassword,
            "3001234567", new DateOnly(1978, 12, 25), null, restrictionIds);

    // --- Registros válidos ---

    [Fact]
    public async Task Handle_WithoutRestrictions_RegistersUser()
    {
        Result<MobileRegistrationResponse> result = await CreateHandler()
            .Handle(CommandWith(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(_writer.Committed);
        Assert.Empty(result.Value.RestrictionIds);
        Assert.Empty(_writer.CommittedUserRestrictions);
        Assert.Equal(nameof(UserStatus.Unverified), result.Value.Status);
    }

    [Fact]
    public async Task Handle_WithOneRestriction_RegistersUser()
    {
        RestrictionId restrictionId = SeedRestriction();

        Result<MobileRegistrationResponse> result = await CreateHandler()
            .Handle(CommandWith(restrictionId.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(_writer.CommittedUserRestrictions);
        Assert.Equal(restrictionId.Value, Assert.Single(result.Value.RestrictionIds));
    }

    [Fact]
    public async Task Handle_WithSeveralRestrictions_RegistersAllOfThem()
    {
        RestrictionId first = SeedRestriction();
        RestrictionId second = SeedRestriction();
        RestrictionId third = SeedRestriction();

        Result<MobileRegistrationResponse> result = await CreateHandler()
            .Handle(CommandWith(first.Value, second.Value, third.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, _writer.CommittedUserRestrictions.Count);
        Assert.Equal(
            [first, second, third],
            _writer.CommittedUserRestrictions.Select(ur => ur.RestrictionId));
    }

    [Fact]
    public async Task Handle_PersistsUserRoleAndRestrictions_InASingleWriterCall()
    {
        RestrictionId restrictionId = SeedRestriction();

        await CreateHandler().Handle(CommandWith(restrictionId.Value), CancellationToken.None);

        Assert.Equal(1, _writer.RegisterCallCount);
        Assert.NotNull(_writer.CommittedUser);
        Assert.NotNull(_writer.CommittedUserRole);
    }

    // --- Correo duplicado ---

    [Fact]
    public async Task Handle_WhenEmailAlreadyExists_ReturnsConflictAndPersistsNothing()
    {
        _userRepository.Seed(User.Create(
            UserId.New(), "Otro", "Usuario", "andres@email.com", "hash", null, null, null));

        Result<MobileRegistrationResponse> result = await CreateHandler()
            .Handle(CommandWith(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("MobileRegistrations.EmailAlreadyExists", result.Error.Code);
        Assert.Equal(0, _writer.RegisterCallCount);
    }

    [Fact]
    public async Task Handle_NormalizesEmailBeforeCheckingDuplicate()
    {
        _userRepository.Seed(User.Create(
            UserId.New(), "Otro", "Usuario", "andres@email.com", "hash", null, null, null));

        var command = new RegisterMobileUserCommand(
            "Andres", "Perez", "  ANDRES@EMAIL.COM  ", PlainPassword, null, null, null, []);

        Result<MobileRegistrationResponse> result = await CreateHandler()
            .Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }

    // --- Configuración del rol base ---

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("no-es-un-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task Handle_WhenDefaultRoleIdIsInvalid_ReturnsControlledFailure(string roleId)
    {
        _defaults.DefaultRoleId = roleId;

        Result<MobileRegistrationResponse> result = await CreateHandler()
            .Handle(CommandWith(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Failure, result.Error.Type);
        Assert.Equal("MobileRegistrations.RoleConfigurationError", result.Error.Code);
        Assert.Equal(0, _writer.RegisterCallCount);
    }

    [Fact]
    public async Task Handle_WhenConfiguredRoleDoesNotExist_ReturnsControlledFailure()
    {
        _defaults.DefaultRoleId = Guid.NewGuid().ToString();

        Result<MobileRegistrationResponse> result = await CreateHandler()
            .Handle(CommandWith(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Failure, result.Error.Type);
        Assert.Equal("MobileRegistrations.RoleConfigurationError", result.Error.Code);
        Assert.Equal(0, _writer.RegisterCallCount);
    }

    [Fact]
    public async Task Handle_WhenConfiguredRoleIsInactive_ReturnsControlledFailure()
    {
        _role.Deactivate();

        Result<MobileRegistrationResponse> result = await CreateHandler()
            .Handle(CommandWith(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Failure, result.Error.Type);
        Assert.Equal("MobileRegistrations.RoleConfigurationError", result.Error.Code);
        Assert.Equal(0, _writer.RegisterCallCount);
    }

    /// <summary>
    /// Los cuatro problemas posibles con el rol configurado deben ser indistinguibles
    /// desde fuera: el cliente móvil no envía ni controla el rol.
    /// </summary>
    [Fact]
    public async Task Handle_AllRoleConfigurationProblems_ProduceTheSameError()
    {
        List<Error> errors = [];

        // 1. Identificador ausente.
        _defaults.DefaultRoleId = "";
        errors.Add((await CreateHandler()
            .Handle(CommandWith(), CancellationToken.None)).Error);

        // 2. Identificador mal formado.
        _defaults.DefaultRoleId = "no-es-un-guid";
        errors.Add((await CreateHandler()
            .Handle(CommandWith(), CancellationToken.None)).Error);

        // 3. Rol inexistente.
        _defaults.DefaultRoleId = Guid.NewGuid().ToString();
        errors.Add((await CreateHandler()
            .Handle(CommandWith(), CancellationToken.None)).Error);

        // 4. Rol inactivo.
        _defaults.DefaultRoleId = _roleId.Value.ToString();
        _role.Deactivate();
        errors.Add((await CreateHandler()
            .Handle(CommandWith(), CancellationToken.None)).Error);

        Assert.Equal(4, errors.Count);
        Assert.All(errors, error => Assert.Equal(errors[0], error));
        Assert.All(errors, error => Assert.Equal(ErrorType.Failure, error.Type));
        Assert.All(
            errors,
            error => Assert.Equal("MobileRegistrations.RoleConfigurationError", error.Code));
    }

    [Fact]
    public async Task Handle_ErrorDescriptions_DoNotLeakConfiguredRoleId()
    {
        string configuredRoleId = Guid.NewGuid().ToString();
        _defaults.DefaultRoleId = configuredRoleId;

        Result<MobileRegistrationResponse> result = await CreateHandler()
            .Handle(CommandWith(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.DoesNotContain(
            configuredRoleId, result.Error.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_RoleConfigurationError_DoesNotRevealWhichProblemOccurred()
    {
        _role.Deactivate();

        Result<MobileRegistrationResponse> result = await CreateHandler()
            .Handle(CommandWith(), CancellationToken.None);

        foreach (string leak in new[] { "inactiv", "no existe", "inexistente", "configurac" })
        {
            Assert.DoesNotContain(
                leak, result.Error.Description, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task Handle_LocatesRoleById_NotByName()
    {
        // Un segundo rol con el mismo nombre no debe alterar la selección:
        // Role.Name no es único y no se usa como identificador técnico.
        var homonym = Role.Create(RoleId.New(), "Usuario", null);
        _roleRepository.Seed(homonym);

        Result<MobileRegistrationResponse> result = await CreateHandler()
            .Handle(CommandWith(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(
            _roleId, Assert.IsType<UserRole>(_writer.CommittedUserRole).RoleId);
    }

    // --- Nivel de importancia predeterminado ---

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Critical")]
    [InlineData("high")]
    public async Task Handle_WhenDefaultImportanceLevelIsInvalid_ReturnsControlledFailure(
        string importanceLevel)
    {
        _defaults.DefaultRestrictionImportanceLevel = importanceLevel;

        Result<MobileRegistrationResponse> result = await CreateHandler()
            .Handle(CommandWith(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Failure, result.Error.Type);
        Assert.Equal("MobileRegistrations.InvalidDefaultImportanceLevel", result.Error.Code);
        Assert.Equal(0, _writer.RegisterCallCount);
    }

    [Theory]
    [InlineData(UserRestrictionImportanceLevels.Low)]
    [InlineData(UserRestrictionImportanceLevels.Medium)]
    [InlineData(UserRestrictionImportanceLevels.High)]
    public async Task Handle_AppliesConfiguredImportanceLevel_ToEveryRestriction(string level)
    {
        _defaults.DefaultRestrictionImportanceLevel = level;
        RestrictionId first = SeedRestriction();
        RestrictionId second = SeedRestriction();

        await CreateHandler()
            .Handle(CommandWith(first.Value, second.Value), CancellationToken.None);

        Assert.All(
            _writer.CommittedUserRestrictions,
            ur => Assert.Equal(level, ur.ImportanceLevel));
    }

    [Fact]
    public async Task Handle_ByDefault_AppliesHighImportanceLevel()
    {
        RestrictionId restrictionId = SeedRestriction();

        await CreateHandler().Handle(CommandWith(restrictionId.Value), CancellationToken.None);

        Assert.Equal(
            UserRestrictionImportanceLevels.High,
            Assert.Single(_writer.CommittedUserRestrictions).ImportanceLevel);
    }

    // --- Restricciones alimenticias ---

    [Fact]
    public async Task Handle_WhenRestrictionDoesNotExist_ReturnsNotFoundAndPersistsNothing()
    {
        RestrictionId existing = SeedRestriction();
        Guid missing = Guid.NewGuid();

        Result<MobileRegistrationResponse> result = await CreateHandler()
            .Handle(CommandWith(existing.Value, missing), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("MobileRegistrations.RestrictionNotFound", result.Error.Code);
        Assert.Equal(0, _writer.RegisterCallCount);
    }

    [Fact]
    public async Task Handle_WhenRestrictionIsInactive_ReturnsNotAvailableAndPersistsNothing()
    {
        RestrictionId inactive = SeedRestriction(isActive: false);

        Result<MobileRegistrationResponse> result = await CreateHandler()
            .Handle(CommandWith(inactive.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("MobileRegistrations.RestrictionNotAvailable", result.Error.Code);
        Assert.Equal(0, _writer.RegisterCallCount);
    }

    [Fact]
    public async Task Handle_ValidatesEveryRestrictionBeforePersisting()
    {
        RestrictionId first = SeedRestriction();
        RestrictionId second = SeedRestriction();

        Result<MobileRegistrationResponse> result = await CreateHandler()
            .Handle(
                CommandWith(first.Value, second.Value, Guid.NewGuid()),
                CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.False(_writer.Committed);
    }

    // --- Contraseña ---

    [Fact]
    public async Task Handle_HashesPassword_AndNeverPersistsPlainText()
    {
        await CreateHandler().Handle(CommandWith(), CancellationToken.None);

        User persisted = Assert.IsType<User>(_writer.CommittedUser);
        Assert.Equal("hashed_" + PlainPassword, persisted.PasswordHash);
        Assert.NotEqual(PlainPassword, persisted.PasswordHash);
    }

    [Fact]
    public async Task Handle_DoesNotExposePasswordInResponse()
    {
        Result<MobileRegistrationResponse> result = await CreateHandler()
            .Handle(CommandWith(), CancellationToken.None);

        string serialized = JsonSerializer.Serialize(result.Value);

        Assert.DoesNotContain(PlainPassword, serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("hashed_", serialized, StringComparison.OrdinalIgnoreCase);
    }

    // --- Estado inicial y TimeProvider ---

    [Fact]
    public async Task Handle_CreatesUser_WithUnverifiedStatusAndUnverifiedEmail()
    {
        await CreateHandler().Handle(CommandWith(), CancellationToken.None);

        User persisted = Assert.IsType<User>(_writer.CommittedUser);
        Assert.Equal(UserStatus.Unverified, persisted.Status);
        Assert.False(persisted.IsEmailVerified);
        Assert.Null(persisted.LastLoginAtUtc);
    }

    [Fact]
    public async Task Handle_UsesTimeProvider_ForUserRoleAndRestrictionDates()
    {
        RestrictionId restrictionId = SeedRestriction();

        await CreateHandler().Handle(CommandWith(restrictionId.Value), CancellationToken.None);

        DateTime expected = FixedNow.UtcDateTime;

        Assert.Equal(expected, Assert.IsType<UserRole>(_writer.CommittedUserRole).AssignedAtUtc);
        Assert.Equal(expected, Assert.Single(_writer.CommittedUserRestrictions).CreatedAtUtc);
    }

    // --- Verificación de correo inicial ---

    [Fact]
    public async Task Handle_CreatesTheInitialEmailVerification()
    {
        await CreateHandler().Handle(CommandWith(), CancellationToken.None);

        UserEmailVerification verification =
            Assert.IsType<UserEmailVerification>(_writer.CommittedEmailVerification);

        User persisted = Assert.IsType<User>(_writer.CommittedUser);

        Assert.Equal(persisted.Id, verification.UserId);
        Assert.False(verification.IsUsed);
        Assert.False(verification.IsRevoked);
        Assert.Equal(0, verification.FailedAttempts);
    }

    [Fact]
    public async Task Handle_UsesTimeProvider_ForTheVerificationDates()
    {
        await CreateHandler().Handle(CommandWith(), CancellationToken.None);

        UserEmailVerification verification =
            Assert.IsType<UserEmailVerification>(_writer.CommittedEmailVerification);

        Assert.Equal(FixedNow.UtcDateTime, verification.CreatedAtUtc);
        Assert.Equal(
            FixedNow.UtcDateTime.AddMinutes(_verificationDefaults.ExpirationMinutes),
            verification.ExpiresAtUtc);
    }

    [Fact]
    public async Task Handle_GeneratesASixDigitCode()
    {
        await CreateHandler().Handle(CommandWith(), CancellationToken.None);

        string code = Assert.Single(_codeGenerator.GeneratedCodes);

        Assert.Equal(6, code.Length);
        Assert.All(code, character => Assert.True(char.IsAsciiDigit(character)));
    }

    /// <summary>
    /// La verificación solo puede llevar el hash: el código de seis dígitos jamás llega
    /// a la persistencia.
    /// </summary>
    [Fact]
    public async Task Handle_PersistsOnlyTheCodeHash_NeverThePlainCode()
    {
        await CreateHandler().Handle(CommandWith(), CancellationToken.None);

        UserEmailVerification verification =
            Assert.IsType<UserEmailVerification>(_writer.CommittedEmailVerification);

        string code = Assert.Single(_codeGenerator.GeneratedCodes);

        Assert.NotEqual(code, verification.CodeHash);
        Assert.DoesNotContain(code, verification.CodeHash, StringComparison.Ordinal);
        Assert.Equal(_codeHasher.ComputeHash(code), verification.CodeHash);
        Assert.Equal(UserEmailVerification.CodeHashLength, verification.CodeHash.Length);
    }

    [Fact]
    public async Task Handle_SendsTheCodeOnlyToTheUserEmail()
    {
        await CreateHandler().Handle(CommandWith(), CancellationToken.None);

        FakeEmailSender.SentEmail sent = Assert.Single(_emailSender.SentEmails);

        Assert.Equal("andres@email.com", sent.RecipientEmail);
        Assert.Equal("Andres", sent.RecipientName);
        Assert.Equal(Assert.Single(_codeGenerator.GeneratedCodes), sent.VerificationCode);
        Assert.Equal(
            Assert.IsType<UserEmailVerification>(_writer.CommittedEmailVerification).ExpiresAtUtc,
            sent.ExpiresAtUtc);
    }

    /// <summary>
    /// El envío nunca puede ocurrir con la transacción abierta: cuando el remitente se
    /// invoca, la escritura ya debe estar confirmada.
    /// </summary>
    [Fact]
    public async Task Handle_SendsTheEmailAfterTheDatabaseWriteIsCommitted()
    {
        bool committedWhenSending = false;
        _emailSender.OnSend = () => committedWhenSending = _writer.Committed;

        await CreateHandler().Handle(CommandWith(), CancellationToken.None);

        Assert.True(committedWhenSending);
    }

    [Fact]
    public async Task Handle_WhenTheVerificationCannotBePersisted_NothingIsCommitted()
    {
        RestrictionId restrictionId = SeedRestriction();
        _writer.FailureToThrow = new InvalidOperationException("fallo al crear la verificación");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateHandler().Handle(CommandWith(restrictionId.Value), CancellationToken.None)
                .AsTask());

        Assert.False(_writer.Committed);
        Assert.Null(_writer.CommittedUser);
        Assert.Null(_writer.CommittedUserRole);
        Assert.Empty(_writer.CommittedUserRestrictions);
        Assert.Null(_writer.CommittedEmailVerification);

        // Tampoco se envía correo de un registro que no llegó a existir.
        Assert.Empty(_emailSender.SentEmails);
    }

    [Fact]
    public async Task Handle_WhenTheEmailFails_TheUserRemainsRegistered()
    {
        _emailSender.FailureToThrow = new InvalidOperationException("fallo del proveedor SMTP");

        Result<MobileRegistrationResponse> result = await CreateHandler()
            .Handle(CommandWith(), CancellationToken.None);

        // El registro conserva su comportamiento: el fallo de envío no lo revierte.
        Assert.True(result.IsSuccess);
        Assert.True(_writer.Committed);
        Assert.NotNull(_writer.CommittedUser);
        Assert.NotNull(_writer.CommittedEmailVerification);
        Assert.Equal(nameof(UserStatus.Unverified), result.Value.Status);
    }

    [Fact]
    public async Task Handle_ResponseNeverContainsTheVerificationCode()
    {
        Result<MobileRegistrationResponse> result = await CreateHandler()
            .Handle(CommandWith(), CancellationToken.None);

        string serialized = JsonSerializer.Serialize(result.Value);
        string code = Assert.Single(_codeGenerator.GeneratedCodes);

        Assert.DoesNotContain(code, serialized, StringComparison.Ordinal);
        Assert.DoesNotContain(
            _codeHasher.ComputeHash(code), serialized, StringComparison.OrdinalIgnoreCase);
    }

    // --- Asignación del rol ---

    [Fact]
    public async Task Handle_AssignsConfiguredRole_WithGlobalScopeAndActive()
    {
        await CreateHandler().Handle(CommandWith(), CancellationToken.None);

        UserRole userRole = Assert.IsType<UserRole>(_writer.CommittedUserRole);
        Assert.Equal(_roleId, userRole.RoleId);
        Assert.Equal(ScopeType.Global, userRole.ScopeType);
        Assert.Null(userRole.EstablishmentId);
        Assert.Null(userRole.BranchId);
        Assert.True(userRole.IsActive);
        Assert.Null(userRole.EndedAtUtc);
    }

    [Fact]
    public async Task Handle_LinksUserRoleAndRestrictions_ToTheCreatedUser()
    {
        RestrictionId restrictionId = SeedRestriction();

        Result<MobileRegistrationResponse> result = await CreateHandler()
            .Handle(CommandWith(restrictionId.Value), CancellationToken.None);

        var userId = new UserId(result.Value.UserId);

        Assert.Equal(userId, Assert.IsType<User>(_writer.CommittedUser).Id);
        Assert.Equal(userId, Assert.IsType<UserRole>(_writer.CommittedUserRole).UserId);
        Assert.Equal(userId, Assert.Single(_writer.CommittedUserRestrictions).UserId);
    }

    // --- Fallo de persistencia ---

    [Fact]
    public async Task Handle_WhenPersistenceFails_PropagatesAndConfirmsNothing()
    {
        RestrictionId restrictionId = SeedRestriction();
        _writer.FailureToThrow = new InvalidOperationException("fallo simulado de base de datos");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateHandler()
                .Handle(CommandWith(restrictionId.Value), CancellationToken.None)
                .AsTask());

        Assert.False(_writer.Committed);
        Assert.Null(_writer.CommittedUser);
        Assert.Null(_writer.CommittedUserRole);
        Assert.Empty(_writer.CommittedUserRestrictions);
    }

    // --- Contrato del comando ---

    [Theory]
    [InlineData("RoleId")]
    [InlineData("RoleName")]
    [InlineData("RoleCode")]
    [InlineData("PasswordHash")]
    [InlineData("Status")]
    [InlineData("IsEmailVerified")]
    [InlineData("ImportanceLevel")]
    [InlineData("ScopeType")]
    [InlineData("EstablishmentId")]
    [InlineData("BranchId")]
    [InlineData("IsAdmin")]
    public void Command_DoesNotExposeBackendControlledMember(string memberName)
    {
        PropertyInfo? property = typeof(RegisterMobileUserCommand).GetProperty(
            memberName,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

        Assert.Null(property);
    }

    [Fact]
    public void Command_ExposesOnlyTheExpectedMembers()
    {
        string[] actual = [.. typeof(RegisterMobileUserCommand)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name)
            .Order(StringComparer.Ordinal)];

        Assert.Equal(
            [
                "BirthDate", "Email", "LastName", "Name",
                "Password", "Phone", "PhotoUrl", "RestrictionIds"
            ],
            actual);
    }
}
