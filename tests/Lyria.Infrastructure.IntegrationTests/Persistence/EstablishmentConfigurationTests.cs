using Lyria.Domain.Establishments;
using Lyria.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Lyria.Infrastructure.IntegrationTests.Persistence;

public sealed class EstablishmentConfigurationTests : IDisposable
{
    private readonly SqliteFixture _fixture = new();

    [Fact]
    public void Schema_Should_Be_Default_Dbo()
    {
        using var context = _fixture.CreateContext();
        var entityType = context.Model.FindEntityType(typeof(Establishment))!;
        Assert.Null(entityType.GetSchema());
    }

    [Fact]
    public void TableName_Should_Be_Establecimientos()
    {
        using var context = _fixture.CreateContext();
        var entityType = context.Model.FindEntityType(typeof(Establishment))!;
        Assert.Equal("Establecimientos", entityType.GetTableName());
    }

    [Fact]
    public void Id_Column_Should_Be_EstablecimientoId()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(Establishment.Id));
        Assert.Equal("EstablecimientoId", property.GetColumnName());
    }

    [Fact]
    public void CategoryId_Column_Should_Be_CategoriaId()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(Establishment.CategoryId));
        Assert.Equal("CategoriaId", property.GetColumnName());
    }

    [Fact]
    public void Name_Column_Should_Be_Nombre()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(Establishment.Name));
        Assert.Equal("Nombre", property.GetColumnName());
    }

    [Fact]
    public void Slug_Column_Should_Be_Slug()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(Establishment.Slug));
        Assert.Equal("Slug", property.GetColumnName());
    }

    [Fact]
    public void Description_Column_Should_Be_Descripcion()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(Establishment.Description));
        Assert.Equal("Descripcion", property.GetColumnName());
    }

    [Fact]
    public void Website_Column_Should_Be_SitioWeb()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(Establishment.Website));
        Assert.Equal("SitioWeb", property.GetColumnName());
    }

    [Fact]
    public void Instagram_Column_Should_Be_Instagram()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(Establishment.Instagram));
        Assert.Equal("Instagram", property.GetColumnName());
    }

    [Fact]
    public void IsVerified_Column_Should_Be_Verificado()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(Establishment.IsVerified));
        Assert.Equal("Verificado", property.GetColumnName());
    }

    [Fact]
    public void IsActive_Column_Should_Be_Activo()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(Establishment.IsActive));
        Assert.Equal("Activo", property.GetColumnName());
    }

    [Fact]
    public void Name_MaxLength_Should_Be_150()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(Establishment.Name));
        Assert.Equal(Establishment.NameMaxLength, property.GetMaxLength());
    }

    [Fact]
    public void Slug_MaxLength_Should_Be_160()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(Establishment.Slug));
        Assert.Equal(Establishment.SlugMaxLength, property.GetMaxLength());
    }

    [Fact]
    public void Description_MaxLength_Should_Be_1000()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(Establishment.Description));
        Assert.Equal(Establishment.DescriptionMaxLength, property.GetMaxLength());
    }

    [Fact]
    public void Website_MaxLength_Should_Be_250()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(Establishment.Website));
        Assert.Equal(Establishment.WebsiteMaxLength, property.GetMaxLength());
    }

    [Fact]
    public void Instagram_MaxLength_Should_Be_200()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(Establishment.Instagram));
        Assert.Equal(Establishment.InstagramMaxLength, property.GetMaxLength());
    }

    [Fact]
    public void Slug_Should_Not_Be_Unicode()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(Establishment.Slug));
        Assert.False(property.IsUnicode());
    }

    [Fact]
    public void IsActive_Should_Be_Required()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(Establishment.IsActive));
        Assert.False(property.IsNullable);
    }

    [Fact]
    public void PrimaryKey_Should_Be_Named_PK_Establecimientos()
    {
        using var context = _fixture.CreateContext();
        var entityType = context.Model.FindEntityType(typeof(Establishment))!;
        var primaryKey = entityType.FindPrimaryKey()!;
        Assert.Equal("PK_Establecimientos", primaryKey.GetName());
    }

    [Fact]
    public void UniqueIndex_On_Slug_Should_Be_Named_UX_Establecimientos_Slug()
    {
        using var context = _fixture.CreateContext();
        var entityType = context.Model.FindEntityType(typeof(Establishment))!;
        var slugProperty = entityType.FindProperty(nameof(Establishment.Slug))!;
        var index = entityType.GetIndexes()
            .First(i => i.Properties.Any(p => p.Name == slugProperty.Name));
        Assert.True(index.IsUnique);
        Assert.Equal("UX_Establecimientos_Slug", index.GetDatabaseName());
    }

    [Fact]
    public void Id_Should_Have_ValueConverter()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(Establishment.Id));
        var converter = property.GetValueConverter();
        Assert.NotNull(converter);
        Assert.Equal(typeof(EstablishmentId), converter.ModelClrType);
        Assert.Equal(typeof(Guid), converter.ProviderClrType);
    }

    [Fact]
    public void Id_Should_Be_ValueGeneratedNever()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(Establishment.Id));
        Assert.Equal(ValueGenerated.Never, property.ValueGenerated);
    }

    [Fact]
    public void IsActive_Should_Not_Have_ValueConverter()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(Establishment.IsActive));
        var converter = property.GetValueConverter();
        Assert.Null(converter);
    }

    [Fact]
    public void DomainEvents_Should_Not_Be_Mapped()
    {
        using var context = _fixture.CreateContext();
        var entityType = context.Model.FindEntityType(typeof(Establishment))!;
        var domainEventsProperty = entityType.FindProperty("DomainEvents");
        Assert.Null(domainEventsProperty);
    }

    [Fact]
    public void LogoUrl_Column_Should_Be_LogoUrl()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(Establishment.LogoUrl));
        Assert.Equal("LogoUrl", property.GetColumnName());
    }

    [Fact]
    public void LogoUrl_MaxLength_Should_Be_500()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(Establishment.LogoUrl));
        Assert.Equal(Establishment.LogoUrlMaxLength, property.GetMaxLength());
    }

    [Fact]
    public void LogoUrl_Should_Not_Be_Unicode()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(Establishment.LogoUrl));
        Assert.False(property.IsUnicode());
    }

    [Fact]
    public void LogoUrl_Should_Be_Optional()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(Establishment.LogoUrl));
        Assert.True(property.IsNullable);
    }

    [Fact]
    public void ContactEmail_Column_Should_Be_EmailContacto()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(Establishment.ContactEmail));
        Assert.Equal("EmailContacto", property.GetColumnName());
    }

    [Fact]
    public void ContactEmail_MaxLength_Should_Be_254()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(Establishment.ContactEmail));
        Assert.Equal(Establishment.ContactEmailMaxLength, property.GetMaxLength());
    }

    [Fact]
    public void ContactEmail_Should_Not_Be_Unicode()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(Establishment.ContactEmail));
        Assert.False(property.IsUnicode());
    }

    [Fact]
    public void ContactEmail_Should_Be_Optional()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(Establishment.ContactEmail));
        Assert.True(property.IsNullable);
    }

    [Fact]
    public void ContactPhone_Column_Should_Be_TelefonoContacto()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(Establishment.ContactPhone));
        Assert.Equal("TelefonoContacto", property.GetColumnName());
    }

    [Fact]
    public void ContactPhone_MaxLength_Should_Be_30()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(Establishment.ContactPhone));
        Assert.Equal(Establishment.ContactPhoneMaxLength, property.GetMaxLength());
    }

    [Fact]
    public void ContactPhone_Should_Not_Be_Unicode()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(Establishment.ContactPhone));
        Assert.False(property.IsUnicode());
    }

    [Fact]
    public void ContactPhone_Should_Be_Optional()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(Establishment.ContactPhone));
        Assert.True(property.IsNullable);
    }

    [Fact]
    public void VerifiedAtUtc_Column_Should_Be_FechaVerificacion()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(Establishment.VerifiedAtUtc));
        Assert.Equal("FechaVerificacion", property.GetColumnName());
    }

    [Fact]
    public void VerifiedAtUtc_Should_Be_Optional()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(Establishment.VerifiedAtUtc));
        Assert.True(property.IsNullable);
    }

    public void Dispose()
    {
        _fixture.Dispose();
    }

    private static IProperty GetProperty(LyriaDbContext context, string propertyName)
    {
        var entityType = context.Model.FindEntityType(typeof(Establishment))!;
        return entityType.FindProperty(propertyName)!;
    }
}
