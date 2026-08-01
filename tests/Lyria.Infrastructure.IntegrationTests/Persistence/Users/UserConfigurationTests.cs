using Lyria.Domain.Users;
using Lyria.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Lyria.Infrastructure.IntegrationTests.Persistence.Users;

public sealed class UserConfigurationTests : IDisposable
{
    private readonly SqliteFixture _fixture = new();

    [Fact]
    public void Table_ShouldBeNamedUsuarios()
    {
        using var context = _fixture.CreateContext();

        var entityType = context.Model.FindEntityType(typeof(User))!;

        Assert.Equal("Usuarios", entityType.GetTableName());
    }

    [Fact]
    public void PrimaryKey_ShouldBeUsuarioId()
    {
        using var context = _fixture.CreateContext();

        var entityType = context.Model.FindEntityType(typeof(User))!;
        var primaryKey = entityType.FindPrimaryKey()!;

        Assert.Equal("PK_Usuarios", primaryKey.GetName());

        var idProperty = entityType.FindProperty(nameof(User.Id))!;
        Assert.Equal("UsuarioId", idProperty.GetColumnName());
    }

    [Fact]
    public void Email_ShouldHaveUniqueIndex()
    {
        using var context = _fixture.CreateContext();

        var entityType = context.Model.FindEntityType(typeof(User))!;
        var indexes = entityType.GetIndexes().ToList();

        var emailIndex = indexes.SingleOrDefault(
            i => i.GetDatabaseName() == "UX_Usuarios_Email");

        Assert.NotNull(emailIndex);
        Assert.True(emailIndex.IsUnique);
    }

    [Fact]
    public void AllColumns_ShouldHaveCorrectTypes()
    {
        using var context = _fixture.CreateContext();

        // Id
        var idProperty = GetProperty(context, nameof(User.Id));
        Assert.Equal("UsuarioId", idProperty.GetColumnName());
        Assert.False(idProperty.IsNullable);
        var converter = idProperty.GetValueConverter();
        Assert.NotNull(converter);
        Assert.Equal(typeof(UserId), converter.ModelClrType);
        Assert.Equal(typeof(Guid), converter.ProviderClrType);
        Assert.Equal(ValueGenerated.Never, idProperty.ValueGenerated);

        // Name
        var nameProperty = GetProperty(context, nameof(User.Name));
        Assert.Equal("Nombre", nameProperty.GetColumnName());
        Assert.Equal(User.NameMaxLength, nameProperty.GetMaxLength());
        Assert.False(nameProperty.IsNullable);

        // LastName
        var lastNameProperty = GetProperty(context, nameof(User.LastName));
        Assert.Equal("Apellido", lastNameProperty.GetColumnName());
        Assert.Equal(User.LastNameMaxLength, lastNameProperty.GetMaxLength());
        Assert.False(lastNameProperty.IsNullable);

        // Email
        var emailProperty = GetProperty(context, nameof(User.Email));
        Assert.Equal("Email", emailProperty.GetColumnName());
        Assert.Equal(User.EmailMaxLength, emailProperty.GetMaxLength());
        Assert.False(emailProperty.IsNullable);
        Assert.False(emailProperty.IsUnicode());

        // PasswordHash
        var passwordHashProperty = GetProperty(context, nameof(User.PasswordHash));
        Assert.Equal("HashContrasena", passwordHashProperty.GetColumnName());
        Assert.Equal(User.PasswordHashMaxLength, passwordHashProperty.GetMaxLength());
        Assert.False(passwordHashProperty.IsNullable);
        Assert.False(passwordHashProperty.IsUnicode());

        // Phone
        var phoneProperty = GetProperty(context, nameof(User.Phone));
        Assert.Equal("Telefono", phoneProperty.GetColumnName());
        Assert.Equal(User.PhoneMaxLength, phoneProperty.GetMaxLength());
        Assert.True(phoneProperty.IsNullable);
        Assert.False(phoneProperty.IsUnicode());

        // BirthDate
        var birthDateProperty = GetProperty(context, nameof(User.BirthDate));
        Assert.Equal("FechaNacimiento", birthDateProperty.GetColumnName());
        Assert.True(birthDateProperty.IsNullable);

        // PhotoUrl
        var photoUrlProperty = GetProperty(context, nameof(User.PhotoUrl));
        Assert.Equal("FotoUrl", photoUrlProperty.GetColumnName());
        Assert.Equal(User.PhotoUrlMaxLength, photoUrlProperty.GetMaxLength());
        Assert.True(photoUrlProperty.IsNullable);

        // Status
        var statusProperty = GetProperty(context, nameof(User.Status));
        Assert.Equal("Estado", statusProperty.GetColumnName());
        Assert.False(statusProperty.IsNullable);

        // IsEmailVerified
        var isEmailVerifiedProperty = GetProperty(context, nameof(User.IsEmailVerified));
        Assert.Equal("EmailVerificado", isEmailVerifiedProperty.GetColumnName());
        Assert.False(isEmailVerifiedProperty.IsNullable);

        // LastLoginAtUtc
        var lastLoginProperty = GetProperty(context, nameof(User.LastLoginAtUtc));
        Assert.Equal("UltimaConexion", lastLoginProperty.GetColumnName());
        Assert.True(lastLoginProperty.IsNullable);

        // CreatedAtUtc
        var createdAtProperty = GetProperty(context, nameof(User.CreatedAtUtc));
        Assert.Equal("FechaCreacion", createdAtProperty.GetColumnName());
        Assert.False(createdAtProperty.IsNullable);

        // UpdatedAtUtc
        var updatedAtProperty = GetProperty(context, nameof(User.UpdatedAtUtc));
        Assert.Equal("FechaActualizacion", updatedAtProperty.GetColumnName());
        Assert.True(updatedAtProperty.IsNullable);
    }

    [Fact]
    public void DomainEvents_Should_Not_Be_Mapped()
    {
        using var context = _fixture.CreateContext();

        var entityType = context.Model.FindEntityType(typeof(User))!;
        var domainEventsProperty = entityType.FindProperty("DomainEvents");

        Assert.Null(domainEventsProperty);
    }

    [Fact]
    public void Email_Should_Enforce_CaseInsensitive_Uniqueness()
    {
        var first = CreateTestUser("unique@example.com");

        using (var context = _fixture.CreateContext())
        {
            context.Set<User>().Add(first);
            context.SaveChanges();
        }

        var duplicate = CreateTestUser("UNIQUE@example.com");

        using (var context = _fixture.CreateContext())
        {
            context.Set<User>().Add(duplicate);
            Assert.ThrowsAny<DbUpdateException>(() => context.SaveChanges());
        }
    }

    public void Dispose()
    {
        _fixture.Dispose();
    }

    private static IProperty GetProperty(LyriaDbContext context, string propertyName)
    {
        var entityType = context.Model.FindEntityType(typeof(User))!;
        return entityType.FindProperty(propertyName)!;
    }

    private static User CreateTestUser(string email = "test@example.com") =>
        User.Create(UserId.New(), "Juan", "Garcia", email, "hashed_password_123", null, null, null);
}
