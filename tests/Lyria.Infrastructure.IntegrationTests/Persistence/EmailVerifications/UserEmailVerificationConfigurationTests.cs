using Lyria.Domain.Users;
using Lyria.Domain.Users.EmailVerifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Lyria.Infrastructure.IntegrationTests.Persistence.EmailVerifications;

public sealed class UserEmailVerificationConfigurationTests : IDisposable
{
    private const string CodeHash =
        "3b8c9f1d2e4a5b6c7d8e9f0a1b2c3d4e5f60718293a4b5c6d7e8f9012a3b4c5d";

    private static readonly DateTime CreatedAtUtc =
        new(2026, 8, 5, 10, 0, 0, DateTimeKind.Utc);

    private static readonly DateTime ExpiresAtUtc = CreatedAtUtc.AddMinutes(15);

    private readonly SqliteFixture _fixture = new();

    private IEntityType GetEntityType()
    {
        using var context = _fixture.CreateContext();
        return context.Model.FindEntityType(typeof(UserEmailVerification))!;
    }

    // --- Mapeo físico ---

    [Fact]
    public void Table_ShouldBeNamedUsuarioVerificacionesCorreo()
    {
        Assert.Equal("UsuarioVerificacionesCorreo", GetEntityType().GetTableName());
    }

    [Fact]
    public void PrimaryKey_ShouldBeVerificacionCorreoId()
    {
        IEntityType entityType = GetEntityType();
        IKey primaryKey = entityType.FindPrimaryKey()!;

        Assert.Equal("PK_UsuarioVerificacionesCorreo", primaryKey.GetName());
        Assert.Equal(
            "VerificacionCorreoId",
            entityType.FindProperty(nameof(UserEmailVerification.Id))!.GetColumnName());
    }

    [Fact]
    public void AllColumns_ShouldUseSpanishNamesAndExpectedTypes()
    {
        IEntityType entityType = GetEntityType();

        IProperty userId = entityType.FindProperty(nameof(UserEmailVerification.UserId))!;
        Assert.Equal("UsuarioId", userId.GetColumnName());
        Assert.False(userId.IsNullable);

        IProperty codeHash = entityType.FindProperty(nameof(UserEmailVerification.CodeHash))!;
        Assert.Equal("CodigoHash", codeHash.GetColumnName());
        Assert.False(codeHash.IsNullable);
        Assert.Equal(UserEmailVerification.CodeHashLength, codeHash.GetMaxLength());
        Assert.False(codeHash.IsUnicode());

        IProperty createdAt =
            entityType.FindProperty(nameof(UserEmailVerification.CreatedAtUtc))!;
        Assert.Equal("FechaCreacion", createdAt.GetColumnName());
        Assert.False(createdAt.IsNullable);
        Assert.Equal("datetime2", createdAt.GetColumnType());

        IProperty expiresAt =
            entityType.FindProperty(nameof(UserEmailVerification.ExpiresAtUtc))!;
        Assert.Equal("FechaExpiracion", expiresAt.GetColumnName());
        Assert.False(expiresAt.IsNullable);
        Assert.Equal("datetime2", expiresAt.GetColumnType());

        IProperty usedAt = entityType.FindProperty(nameof(UserEmailVerification.UsedAtUtc))!;
        Assert.Equal("FechaUso", usedAt.GetColumnName());
        Assert.True(usedAt.IsNullable);
        Assert.Equal("datetime2", usedAt.GetColumnType());

        IProperty revokedAt =
            entityType.FindProperty(nameof(UserEmailVerification.RevokedAtUtc))!;
        Assert.Equal("FechaRevocacion", revokedAt.GetColumnName());
        Assert.True(revokedAt.IsNullable);
        Assert.Equal("datetime2", revokedAt.GetColumnType());

        IProperty failedAttempts =
            entityType.FindProperty(nameof(UserEmailVerification.FailedAttempts))!;
        Assert.Equal("IntentosFallidos", failedAttempts.GetColumnName());
        Assert.False(failedAttempts.IsNullable);
    }

    /// <summary>
    /// El conjunto de columnas es exactamente el autorizado: ni una más, y ninguna que
    /// pueda contener el código en claro.
    /// </summary>
    [Fact]
    public void Table_ShouldNotExposeAnyPlainCodeColumn()
    {
        string[] columns = [.. GetEntityType()
            .GetProperties()
            .Select(p => p.GetColumnName())
            .Order(StringComparer.Ordinal)];

        Assert.Equal(
            ["CodigoHash", "FechaCreacion", "FechaExpiracion", "FechaRevocacion",
             "FechaUso", "IntentosFallidos", "UsuarioId", "VerificacionCorreoId"],
            columns);
    }

    // --- Índices ---

    [Fact]
    public void UserId_ShouldHaveANonUniqueIndex()
    {
        IIndex? index = GetEntityType().GetIndexes()
            .SingleOrDefault(i =>
                i.GetDatabaseName() == "IX_UsuarioVerificacionesCorreo_UsuarioId");

        Assert.NotNull(index);
        Assert.False(index.IsUnique);
    }

    /// <summary>
    /// No hay índice único sobre el hash: un usuario acumula verificaciones históricas y
    /// dos usuarios distintos podrían generar el mismo código.
    /// </summary>
    [Fact]
    public void CodeHash_ShouldNotHaveAUniqueIndex()
    {
        Assert.DoesNotContain(GetEntityType().GetIndexes(), i => i.IsUnique);
    }

    // --- Clave foránea ---

    [Fact]
    public void UserId_ShouldHaveRestrictForeignKeyToUsuarios()
    {
        IForeignKey foreignKey = GetEntityType().GetForeignKeys().Single();

        Assert.Equal(
            "FK_UsuarioVerificacionesCorreo_Usuarios_UsuarioId",
            foreignKey.GetConstraintName());
        Assert.Equal("Usuarios", foreignKey.PrincipalEntityType.GetTableName());
        Assert.Equal("UsuarioId", foreignKey.PrincipalKey.Properties[0].GetColumnName());
        Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior);
    }

    /// <summary>
    /// La relación es de uno a muchos: cada reenvío deja una fila más.
    /// </summary>
    [Fact]
    public void TheRelationship_IsOneToMany()
    {
        Assert.False(GetEntityType().GetForeignKeys().Single().IsUnique);
    }

    // --- Token de concurrencia ---

    /// <summary>
    /// <c>FechaUso</c> participa en el WHERE de todo UPDATE, que es lo que impide que dos
    /// confirmaciones simultáneas canjeen el mismo código.
    /// </summary>
    [Fact]
    public void UsedAt_IsAConcurrencyToken()
    {
        Assert.True(GetEntityType()
            .FindProperty(nameof(UserEmailVerification.UsedAtUtc))!
            .IsConcurrencyToken);
    }

    // --- Comportamiento relacional real ---

    [Fact]
    public async Task Insert_WithUnknownUser_IsRejectedByTheForeignKey()
    {
        await using var context = _fixture.CreateContext();

        context.Set<UserEmailVerification>().Add(UserEmailVerification.Create(
            UserEmailVerificationId.New(), UserId.New(), CodeHash, CreatedAtUtc, ExpiresAtUtc));

        await Assert.ThrowsAnyAsync<DbUpdateException>(
            () => context.SaveChangesAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeletingAUserWithVerifications_IsRejected()
    {
        UserId userId = await SeedUserAsync();
        await SeedVerificationAsync(userId);

        await using var context = _fixture.CreateContext();

        User user = await context.Set<User>()
            .SingleAsync(u => u.Id == userId, TestContext.Current.CancellationToken);

        context.Set<User>().Remove(user);

        // Restrict: la verificación no se borra en cascada y el borrado se bloquea.
        await Assert.ThrowsAnyAsync<DbUpdateException>(
            () => context.SaveChangesAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task AUserCanAccumulateSeveralVerifications()
    {
        UserId userId = await SeedUserAsync();

        await SeedVerificationAsync(userId);
        await SeedVerificationAsync(userId);

        await using var context = _fixture.CreateContext();

        Assert.Equal(2, await context.Set<UserEmailVerification>()
            .CountAsync(v => v.UserId == userId, TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// Lo almacenado es el hash, nunca el código de seis dígitos.
    /// </summary>
    [Fact]
    public async Task StoredRow_ContainsTheHashAndNotThePlainCode()
    {
        const string plainCode = "482731";

        UserId userId = await SeedUserAsync();
        await SeedVerificationAsync(userId);

        await using var context = _fixture.CreateContext();

        UserEmailVerification stored = await context.Set<UserEmailVerification>()
            .SingleAsync(TestContext.Current.CancellationToken);

        Assert.Equal(CodeHash, stored.CodeHash);
        Assert.DoesNotContain(plainCode, stored.CodeHash, StringComparison.Ordinal);
        Assert.Equal(UserEmailVerification.CodeHashLength, stored.CodeHash.Length);
    }

    [Fact]
    public async Task StoredRow_RoundTripsEveryValue()
    {
        UserId userId = await SeedUserAsync();
        await SeedVerificationAsync(userId);

        await using var context = _fixture.CreateContext();

        UserEmailVerification stored = await context.Set<UserEmailVerification>()
            .SingleAsync(TestContext.Current.CancellationToken);

        Assert.Equal(userId, stored.UserId);
        Assert.Equal(CreatedAtUtc, stored.CreatedAtUtc);
        Assert.Equal(ExpiresAtUtc, stored.ExpiresAtUtc);
        Assert.Null(stored.UsedAtUtc);
        Assert.Null(stored.RevokedAtUtc);
        Assert.Equal(0, stored.FailedAttempts);
    }

    private async Task<UserId> SeedUserAsync()
    {
        var user = User.Create(
            UserId.New(), "Andres", "Perez", $"{Guid.NewGuid():N}@email.com",
            "AQAAAAIAAYagAAAAEHashSimuladoDePruebas==", null, null, null);

        await using var context = _fixture.CreateContext();

        context.Set<User>().Add(user);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        return user.Id;
    }

    private async Task SeedVerificationAsync(UserId userId)
    {
        await using var context = _fixture.CreateContext();

        context.Set<UserEmailVerification>().Add(UserEmailVerification.Create(
            UserEmailVerificationId.New(), userId, CodeHash, CreatedAtUtc, ExpiresAtUtc));

        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    public void Dispose() => _fixture.Dispose();
}
