using Lyria.Domain.Users.UserRestrictions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Lyria.Infrastructure.IntegrationTests.Persistence.UserRestrictions;

public sealed class UserRestrictionConfigurationTests : IDisposable
{
    private readonly SqliteFixture _fixture = new();

    private IEntityType GetEntityType()
    {
        using var context = _fixture.CreateContext();
        return context.Model.FindEntityType(typeof(UserRestriction))!;
    }

    [Fact]
    public void Table_ShouldBeNamedUsuarioRestricciones()
    {
        Assert.Equal("UsuarioRestricciones", GetEntityType().GetTableName());
    }

    [Fact]
    public void Table_ShouldUseDboSchema()
    {
        Assert.Equal("dbo", GetEntityType().GetSchema());
    }

    [Fact]
    public void PrimaryKey_ShouldBeComposite_UsuarioId_RestriccionId()
    {
        IEntityType entityType = GetEntityType();
        IKey primaryKey = entityType.FindPrimaryKey()!;

        Assert.Equal("PK_UsuarioRestricciones", primaryKey.GetName());
        Assert.Equal(
            [nameof(UserRestriction.UserId), nameof(UserRestriction.RestrictionId)],
            primaryKey.Properties.Select(p => p.Name));
    }

    [Fact]
    public void Columns_ShouldUseSpanishNames()
    {
        IEntityType entityType = GetEntityType();

        Assert.Equal(
            "UsuarioId",
            entityType.FindProperty(nameof(UserRestriction.UserId))!.GetColumnName());
        Assert.Equal(
            "RestriccionId",
            entityType.FindProperty(nameof(UserRestriction.RestrictionId))!.GetColumnName());
        Assert.Equal(
            "NivelImportancia",
            entityType.FindProperty(nameof(UserRestriction.ImportanceLevel))!.GetColumnName());
        Assert.Equal(
            "FechaCreacion",
            entityType.FindProperty(nameof(UserRestriction.CreatedAtUtc))!.GetColumnName());
    }

    [Fact]
    public void ImportanceLevel_ShouldBeRequired_NonUnicode_MaxLength10()
    {
        IProperty property = GetEntityType()
            .FindProperty(nameof(UserRestriction.ImportanceLevel))!;

        Assert.False(property.IsNullable);
        Assert.False(property.IsUnicode());
        Assert.Equal(10, property.GetMaxLength());
    }

    [Fact]
    public void CreatedAtUtc_ShouldBeRequired()
    {
        IProperty property = GetEntityType()
            .FindProperty(nameof(UserRestriction.CreatedAtUtc))!;

        Assert.False(property.IsNullable);
    }

    [Fact]
    public void Should_Have_FK_To_Usuarios()
    {
        IForeignKey? foreignKey = GetEntityType().GetForeignKeys()
            .SingleOrDefault(fk =>
                fk.GetConstraintName() == "FK_UsuarioRestricciones_Usuarios_UsuarioId");

        Assert.NotNull(foreignKey);
        Assert.Equal("Usuarios", foreignKey.PrincipalEntityType.GetTableName());
    }

    [Fact]
    public void Should_Have_FK_To_Restricciones()
    {
        IForeignKey? foreignKey = GetEntityType().GetForeignKeys()
            .SingleOrDefault(fk =>
                fk.GetConstraintName() == "FK_UsuarioRestricciones_Restricciones_RestriccionId");

        Assert.NotNull(foreignKey);
        Assert.Equal("Restricciones", foreignKey.PrincipalEntityType.GetTableName());
    }

    [Fact]
    public void ForeignKeys_ShouldUseRestrictDeleteBehavior()
    {
        IEntityType entityType = GetEntityType();

        Assert.All(
            entityType.GetForeignKeys(),
            fk => Assert.Equal(DeleteBehavior.Restrict, fk.DeleteBehavior));
    }

    [Fact]
    public void Should_Have_Only_Two_ForeignKeys()
    {
        Assert.Equal(2, GetEntityType().GetForeignKeys().Count());
    }

    public void Dispose()
    {
        _fixture.Dispose();
    }
}
