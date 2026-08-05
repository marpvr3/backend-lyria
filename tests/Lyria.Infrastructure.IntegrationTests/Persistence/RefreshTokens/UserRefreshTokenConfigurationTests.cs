using Lyria.Domain.Users;
using Lyria.Domain.Users.RefreshTokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Lyria.Infrastructure.IntegrationTests.Persistence.RefreshTokens;

public sealed class UserRefreshTokenConfigurationTests : IDisposable
{
    private const string TokenHash =
        "3b8c9f1d2e4a5b6c7d8e9f0a1b2c3d4e5f60718293a4b5c6d7e8f9012a3b4c5d";

    private static readonly DateTime CreatedAtUtc =
        new(2026, 8, 4, 23, 0, 0, DateTimeKind.Utc);

    private static readonly DateTime ExpiresAtUtc =
        new(2026, 9, 3, 23, 0, 0, DateTimeKind.Utc);

    private readonly SqliteFixture _fixture = new();

    private IEntityType GetEntityType()
    {
        using var context = _fixture.CreateContext();
        return context.Model.FindEntityType(typeof(UserRefreshToken))!;
    }

    // --- Mapeo físico ---

    [Fact]
    public void Table_ShouldBeNamedUsuarioRefreshTokens()
    {
        Assert.Equal("UsuarioRefreshTokens", GetEntityType().GetTableName());
    }

    [Fact]
    public void PrimaryKey_ShouldBeRefreshTokenId()
    {
        IEntityType entityType = GetEntityType();
        IKey primaryKey = entityType.FindPrimaryKey()!;

        Assert.Equal("PK_UsuarioRefreshTokens", primaryKey.GetName());
        Assert.Equal(
            "RefreshTokenId",
            entityType.FindProperty(nameof(UserRefreshToken.Id))!.GetColumnName());
    }

    [Fact]
    public void AllColumns_ShouldUseSpanishNamesAndExpectedTypes()
    {
        IEntityType entityType = GetEntityType();

        IProperty userId = entityType.FindProperty(nameof(UserRefreshToken.UserId))!;
        Assert.Equal("UsuarioId", userId.GetColumnName());
        Assert.False(userId.IsNullable);

        IProperty tokenHash = entityType.FindProperty(nameof(UserRefreshToken.TokenHash))!;
        Assert.Equal("TokenHash", tokenHash.GetColumnName());
        Assert.False(tokenHash.IsNullable);
        Assert.Equal(UserRefreshToken.TokenHashLength, tokenHash.GetMaxLength());
        Assert.False(tokenHash.IsUnicode());

        IProperty createdAt = entityType.FindProperty(nameof(UserRefreshToken.CreatedAtUtc))!;
        Assert.Equal("FechaCreacion", createdAt.GetColumnName());
        Assert.False(createdAt.IsNullable);
        Assert.Equal("datetime2", createdAt.GetColumnType());

        IProperty expiresAt = entityType.FindProperty(nameof(UserRefreshToken.ExpiresAtUtc))!;
        Assert.Equal("FechaExpiracion", expiresAt.GetColumnName());
        Assert.False(expiresAt.IsNullable);
        Assert.Equal("datetime2", expiresAt.GetColumnType());

        IProperty revokedAt = entityType.FindProperty(nameof(UserRefreshToken.RevokedAtUtc))!;
        Assert.Equal("FechaRevocacion", revokedAt.GetColumnName());
        Assert.True(revokedAt.IsNullable);
        Assert.Equal("datetime2", revokedAt.GetColumnType());
    }

    /// <summary>
    /// La tabla no debe tener ninguna columna que pueda contener el token en claro.
    /// </summary>
    [Fact]
    public void Table_ShouldNotExposeAnyPlainTokenColumn()
    {
        string[] columns = [.. GetEntityType()
            .GetProperties()
            .Select(p => p.GetColumnName())
            .Order(StringComparer.Ordinal)];

        // El conjunto de columnas es exactamente el autorizado: ni una más.
        Assert.Equal(
            ["FechaCreacion", "FechaExpiracion", "FechaRevocacion",
             "RefreshTokenId", "TokenHash", "UsuarioId"],
            columns);
    }

    // --- Índices ---

    [Fact]
    public void TokenHash_ShouldHaveAUniqueIndex()
    {
        IIndex? index = GetEntityType().GetIndexes()
            .SingleOrDefault(i => i.GetDatabaseName() == "UX_UsuarioRefreshTokens_TokenHash");

        Assert.NotNull(index);
        Assert.True(index.IsUnique);
    }

    [Fact]
    public void UserId_ShouldHaveANonUniqueIndex()
    {
        IIndex? index = GetEntityType().GetIndexes()
            .SingleOrDefault(i => i.GetDatabaseName() == "IX_UsuarioRefreshTokens_UsuarioId");

        Assert.NotNull(index);
        Assert.False(index.IsUnique);
    }

    // --- Clave foránea ---

    [Fact]
    public void UserId_ShouldHaveRestrictForeignKeyToUsuarios()
    {
        IForeignKey foreignKey = GetEntityType().GetForeignKeys().Single();

        Assert.Equal(
            "FK_UsuarioRefreshTokens_Usuarios_UsuarioId",
            foreignKey.GetConstraintName());
        Assert.Equal("Usuarios", foreignKey.PrincipalEntityType.GetTableName());
        Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior);
    }

    // --- Comportamiento relacional real ---

    [Fact]
    public async Task Insert_WithUnknownUser_IsRejectedByTheForeignKey()
    {
        await using var context = _fixture.CreateContext();

        context.Set<UserRefreshToken>().Add(UserRefreshToken.Create(
            UserRefreshTokenId.New(), UserId.New(), TokenHash, CreatedAtUtc, ExpiresAtUtc));

        await Assert.ThrowsAnyAsync<DbUpdateException>(
            () => context.SaveChangesAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Insert_WithDuplicateTokenHash_IsRejectedByTheUniqueIndex()
    {
        UserId userId = await SeedUserAsync();

        await using (var context = _fixture.CreateContext())
        {
            context.Set<UserRefreshToken>().Add(UserRefreshToken.Create(
                UserRefreshTokenId.New(), userId, TokenHash, CreatedAtUtc, ExpiresAtUtc));

            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using (var context = _fixture.CreateContext())
        {
            context.Set<UserRefreshToken>().Add(UserRefreshToken.Create(
                UserRefreshTokenId.New(), userId, TokenHash, CreatedAtUtc, ExpiresAtUtc));

            await Assert.ThrowsAnyAsync<DbUpdateException>(
                () => context.SaveChangesAsync(TestContext.Current.CancellationToken));
        }
    }

    [Fact]
    public async Task DeletingAUserWithSessions_IsRejected()
    {
        UserId userId = await SeedUserAsync();

        await using (var context = _fixture.CreateContext())
        {
            context.Set<UserRefreshToken>().Add(UserRefreshToken.Create(
                UserRefreshTokenId.New(), userId, TokenHash, CreatedAtUtc, ExpiresAtUtc));

            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using (var context = _fixture.CreateContext())
        {
            User user = await context.Set<User>()
                .SingleAsync(u => u.Id == userId, TestContext.Current.CancellationToken);

            context.Set<User>().Remove(user);

            // Restrict: la sesión no se borra en cascada y el borrado se bloquea.
            await Assert.ThrowsAnyAsync<DbUpdateException>(
                () => context.SaveChangesAsync(TestContext.Current.CancellationToken));
        }
    }

    /// <summary>
    /// Lo almacenado es el hash, nunca el token que recibió el cliente.
    /// </summary>
    [Fact]
    public async Task StoredRow_ContainsTheHashAndNotThePlainToken()
    {
        const string plainToken = "token-opaco-de-prueba-en-claro";

        UserId userId = await SeedUserAsync();

        await using (var context = _fixture.CreateContext())
        {
            context.Set<UserRefreshToken>().Add(UserRefreshToken.Create(
                UserRefreshTokenId.New(), userId, TokenHash, CreatedAtUtc, ExpiresAtUtc));

            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using (var context = _fixture.CreateContext())
        {
            UserRefreshToken stored = await context.Set<UserRefreshToken>()
                .SingleAsync(TestContext.Current.CancellationToken);

            Assert.Equal(TokenHash, stored.TokenHash);
            Assert.DoesNotContain(plainToken, stored.TokenHash, StringComparison.Ordinal);
        }
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

    public void Dispose() => _fixture.Dispose();
}
