using Lyria.Domain.Establishments.Categories;
using Lyria.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Lyria.Infrastructure.IntegrationTests.Persistence;

public sealed class EstablishmentCategoryConfigurationTests : IDisposable
{
    private readonly SqliteFixture _fixture = new();

    [Fact]
    public void Schema_Should_Be_Default_Dbo()
    {
        using var context = _fixture.CreateContext();

        var entityType = context.Model.FindEntityType(typeof(EstablishmentCategory))!;

        Assert.Null(entityType.GetSchema());
    }

    [Fact]
    public void TableName_Should_Be_CategoriasEstablecimiento()
    {
        using var context = _fixture.CreateContext();

        var entityType = context.Model.FindEntityType(typeof(EstablishmentCategory))!;

        Assert.Equal("CategoriasEstablecimiento", entityType.GetTableName());
    }

    [Fact]
    public void Id_Column_Should_Be_CategoriaEstablecimientoId()
    {
        using var context = _fixture.CreateContext();

        var property = GetProperty(context, nameof(EstablishmentCategory.Id));

        Assert.Equal("CategoriaEstablecimientoId", property.GetColumnName());
    }

    [Fact]
    public void Name_Column_Should_Be_Nombre()
    {
        using var context = _fixture.CreateContext();

        var property = GetProperty(context, nameof(EstablishmentCategory.Name));

        Assert.Equal("Nombre", property.GetColumnName());
    }

    [Fact]
    public void Description_Column_Should_Be_Descripcion()
    {
        using var context = _fixture.CreateContext();

        var property = GetProperty(context, nameof(EstablishmentCategory.Description));

        Assert.Equal("Descripcion", property.GetColumnName());
    }

    [Fact]
    public void SortOrder_Column_Should_Be_Orden()
    {
        using var context = _fixture.CreateContext();

        var property = GetProperty(context, nameof(EstablishmentCategory.SortOrder));

        Assert.Equal("Orden", property.GetColumnName());
    }

    [Fact]
    public void IsActive_Column_Should_Be_Activo()
    {
        using var context = _fixture.CreateContext();

        var property = GetProperty(context, nameof(EstablishmentCategory.IsActive));

        Assert.Equal("Activo", property.GetColumnName());
    }

    [Fact]
    public void Name_MaxLength_Should_Be_100()
    {
        using var context = _fixture.CreateContext();

        var property = GetProperty(context, nameof(EstablishmentCategory.Name));

        Assert.Equal(EstablishmentCategory.NameMaxLength, property.GetMaxLength());
    }

    [Fact]
    public void Description_MaxLength_Should_Be_500()
    {
        using var context = _fixture.CreateContext();

        var property = GetProperty(context, nameof(EstablishmentCategory.Description));

        Assert.Equal(EstablishmentCategory.DescriptionMaxLength, property.GetMaxLength());
    }

    [Fact]
    public void PrimaryKey_Should_Be_Named_PK_CategoriasEstablecimiento()
    {
        using var context = _fixture.CreateContext();

        var entityType = context.Model.FindEntityType(typeof(EstablishmentCategory))!;
        var primaryKey = entityType.FindPrimaryKey()!;

        Assert.Equal("PK_CategoriasEstablecimiento", primaryKey.GetName());
    }

    [Fact]
    public void Code_Property_Should_Not_Exist()
    {
        using var context = _fixture.CreateContext();

        var entityType = context.Model.FindEntityType(typeof(EstablishmentCategory))!;
        var codeProperty = entityType.FindProperty("Code");

        Assert.Null(codeProperty);
    }

    [Fact]
    public void No_Index_On_Codigo_Should_Exist()
    {
        using var context = _fixture.CreateContext();

        var entityType = context.Model.FindEntityType(typeof(EstablishmentCategory))!;
        var indexes = entityType.GetIndexes().ToList();

        Assert.DoesNotContain(indexes, i => i.GetDatabaseName() == "UX_CategoriasEstablecimiento_Codigo");
    }

    [Fact]
    public void Id_Should_Have_ValueConverter()
    {
        using var context = _fixture.CreateContext();

        var property = GetProperty(context, nameof(EstablishmentCategory.Id));
        var converter = property.GetValueConverter();

        Assert.NotNull(converter);
        Assert.Equal(typeof(EstablishmentCategoryId), converter.ModelClrType);
        Assert.Equal(typeof(Guid), converter.ProviderClrType);
    }

    [Fact]
    public void Id_Should_Be_ValueGeneratedNever()
    {
        using var context = _fixture.CreateContext();

        var property = GetProperty(context, nameof(EstablishmentCategory.Id));

        Assert.Equal(ValueGenerated.Never, property.ValueGenerated);
    }

    [Fact]
    public void DomainEvents_Should_Not_Be_Mapped()
    {
        using var context = _fixture.CreateContext();

        var entityType = context.Model.FindEntityType(typeof(EstablishmentCategory))!;
        var domainEventsProperty = entityType.FindProperty("DomainEvents");

        Assert.Null(domainEventsProperty);
    }

    [Fact]
    public void IconUrl_Column_Should_Be_IconoUrl()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(EstablishmentCategory.IconUrl));
        Assert.Equal("IconoUrl", property.GetColumnName());
    }

    [Fact]
    public void IconUrl_MaxLength_Should_Be_500()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(EstablishmentCategory.IconUrl));
        Assert.Equal(EstablishmentCategory.IconUrlMaxLength, property.GetMaxLength());
    }

    [Fact]
    public void IconUrl_Should_Not_Be_Unicode()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(EstablishmentCategory.IconUrl));
        Assert.False(property.IsUnicode());
    }

    [Fact]
    public void IconUrl_Should_Be_Optional()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(EstablishmentCategory.IconUrl));
        Assert.True(property.IsNullable);
    }

    [Fact]
    public void Name_Should_Not_Be_Unicode()
    {
        using var context = _fixture.CreateContext();

        var property = GetProperty(context, nameof(EstablishmentCategory.Name));

        Assert.False(property.IsUnicode());
    }

    [Fact]
    public void Name_Should_Enforce_CaseInsensitive_Uniqueness()
    {
        var first = EstablishmentCategory.Create(
            EstablishmentCategoryId.New(), "Restaurante", null, null, 1);

        using (var context = _fixture.CreateContext())
        {
            context.Set<EstablishmentCategory>().Add(first);
            context.SaveChanges();
        }

        var duplicate = EstablishmentCategory.Create(
            EstablishmentCategoryId.New(), "restaurante", null, null, 2);

        using (var context = _fixture.CreateContext())
        {
            context.Set<EstablishmentCategory>().Add(duplicate);
            Assert.ThrowsAny<DbUpdateException>(() => context.SaveChanges());
        }
    }

    [Fact]
    public void UniqueIndex_On_Nombre_Should_Exist()
    {
        using var context = _fixture.CreateContext();

        var entityType = context.Model.FindEntityType(typeof(EstablishmentCategory))!;
        var indexes = entityType.GetIndexes().ToList();

        var nombreIndex = indexes.SingleOrDefault(
            i => i.GetDatabaseName() == "UX_CategoriasEstablecimiento_Nombre");

        Assert.NotNull(nombreIndex);
        Assert.True(nombreIndex.IsUnique);
    }

    public void Dispose()
    {
        _fixture.Dispose();
    }

    private static IProperty GetProperty(LyriaDbContext context, string propertyName)
    {
        var entityType = context.Model.FindEntityType(typeof(EstablishmentCategory))!;
        return entityType.FindProperty(propertyName)!;
    }
}
