using Lyria.Domain.Establishments.Branches;
using Lyria.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Lyria.Infrastructure.IntegrationTests.Persistence;

public sealed class EstablishmentBranchConfigurationTests : IDisposable
{
    private readonly SqliteFixture _fixture = new();

    [Fact]
    public void TableName_Should_Be_Sedes()
    {
        using var context = _fixture.CreateContext();
        var entityType = context.Model.FindEntityType(typeof(EstablishmentBranch))!;
        Assert.Equal("Sedes", entityType.GetTableName());
    }

    [Fact]
    public void Schema_Should_Be_Default_Dbo()
    {
        using var context = _fixture.CreateContext();
        var entityType = context.Model.FindEntityType(typeof(EstablishmentBranch))!;
        Assert.Null(entityType.GetSchema());
    }

    [Fact]
    public void Id_Column_Should_Be_SedeId()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(EstablishmentBranch.Id));
        Assert.Equal("SedeId", property.GetColumnName());
    }

    [Fact]
    public void EstablishmentId_Column_Should_Be_EstablecimientoId()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(EstablishmentBranch.EstablishmentId));
        Assert.Equal("EstablecimientoId", property.GetColumnName());
    }

    [Fact]
    public void Name_Column_Should_Be_Nombre()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(EstablishmentBranch.Name));
        Assert.Equal("Nombre", property.GetColumnName());
    }

    [Fact]
    public void Street_Column_Should_Be_Calle()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(EstablishmentBranch.Street));
        Assert.Equal("Calle", property.GetColumnName());
    }

    [Fact]
    public void Number_Column_Should_Be_Numero()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(EstablishmentBranch.Number));
        Assert.Equal("Numero", property.GetColumnName());
    }

    [Fact]
    public void AddressComplement_Column_Should_Be_ComplementoDireccion()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(EstablishmentBranch.AddressComplement));
        Assert.Equal("ComplementoDireccion", property.GetColumnName());
    }

    [Fact]
    public void Neighborhood_Column_Should_Be_Barrio()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(EstablishmentBranch.Neighborhood));
        Assert.Equal("Barrio", property.GetColumnName());
    }

    [Fact]
    public void City_Column_Should_Be_Ciudad()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(EstablishmentBranch.City));
        Assert.Equal("Ciudad", property.GetColumnName());
    }

    [Fact]
    public void Province_Column_Should_Be_Provincia()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(EstablishmentBranch.Province));
        Assert.Equal("Provincia", property.GetColumnName());
    }

    [Fact]
    public void PostalCode_Column_Should_Be_CodigoPostal()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(EstablishmentBranch.PostalCode));
        Assert.Equal("CodigoPostal", property.GetColumnName());
    }

    [Fact]
    public void Country_Column_Should_Be_Pais()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(EstablishmentBranch.Country));
        Assert.Equal("Pais", property.GetColumnName());
    }

    [Fact]
    public void Latitude_Column_Should_Be_Latitud()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(EstablishmentBranch.Latitude));
        Assert.Equal("Latitud", property.GetColumnName());
    }

    [Fact]
    public void Longitude_Column_Should_Be_Longitud()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(EstablishmentBranch.Longitude));
        Assert.Equal("Longitud", property.GetColumnName());
    }

    [Fact]
    public void Phone_Column_Should_Be_Telefono()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(EstablishmentBranch.Phone));
        Assert.Equal("Telefono", property.GetColumnName());
    }

    [Fact]
    public void WhatsApp_Column_Should_Be_Whatsapp()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(EstablishmentBranch.WhatsApp));
        Assert.Equal("Whatsapp", property.GetColumnName());
    }

    [Fact]
    public void Email_Column_Should_Be_Email()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(EstablishmentBranch.Email));
        Assert.Equal("Email", property.GetColumnName());
    }

    [Fact]
    public void RatingAverage_Column_Should_Be_RatingPromedio()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(EstablishmentBranch.RatingAverage));
        Assert.Equal("RatingPromedio", property.GetColumnName());
    }

    [Fact]
    public void TotalReviews_Column_Should_Be_TotalResenas()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(EstablishmentBranch.TotalReviews));
        Assert.Equal("TotalResenas", property.GetColumnName());
    }

    [Fact]
    public void IsActive_Column_Should_Be_Activo()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(EstablishmentBranch.IsActive));
        Assert.Equal("Activo", property.GetColumnName());
    }

    [Fact]
    public void CreatedAtUtc_Column_Should_Be_FechaCreacion()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(EstablishmentBranch.CreatedAtUtc));
        Assert.Equal("FechaCreacion", property.GetColumnName());
    }

    [Fact]
    public void UpdatedAtUtc_Column_Should_Be_FechaActualizacion()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(EstablishmentBranch.UpdatedAtUtc));
        Assert.Equal("FechaActualizacion", property.GetColumnName());
    }

    [Fact]
    public void PrimaryKey_Should_Be_Named_PK_Sedes()
    {
        using var context = _fixture.CreateContext();
        var entityType = context.Model.FindEntityType(typeof(EstablishmentBranch))!;
        var primaryKey = entityType.FindPrimaryKey()!;
        Assert.Equal("PK_Sedes", primaryKey.GetName());
    }

    [Fact]
    public void UniqueIndex_On_EstablishmentId_Name()
    {
        using var context = _fixture.CreateContext();
        var entityType = context.Model.FindEntityType(typeof(EstablishmentBranch))!;
        var index = entityType.GetIndexes()
            .First(i => i.Properties.Any(p => p.Name == nameof(EstablishmentBranch.EstablishmentId))
                     && i.Properties.Any(p => p.Name == nameof(EstablishmentBranch.Name)));
        Assert.True(index.IsUnique);
        Assert.Equal("UX_Sedes_EstablecimientoId_Nombre", index.GetDatabaseName());
    }

    [Fact]
    public void FK_Should_Be_Restrict()
    {
        using var context = _fixture.CreateContext();
        var entityType = context.Model.FindEntityType(typeof(EstablishmentBranch))!;
        var foreignKey = entityType.GetForeignKeys().First();
        Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior);
    }

    [Fact]
    public void Id_Should_Have_ValueConverter()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(EstablishmentBranch.Id));
        var converter = property.GetValueConverter();
        Assert.NotNull(converter);
        Assert.Equal(typeof(EstablishmentBranchId), converter.ModelClrType);
        Assert.Equal(typeof(Guid), converter.ProviderClrType);
    }

    [Fact]
    public void Id_Should_Be_ValueGeneratedNever()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(EstablishmentBranch.Id));
        Assert.Equal(ValueGenerated.Never, property.ValueGenerated);
    }

    [Fact]
    public void IsActive_Should_Be_Required()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(EstablishmentBranch.IsActive));
        Assert.False(property.IsNullable);
    }

    public void Dispose()
    {
        _fixture.Dispose();
    }

    private static IProperty GetProperty(LyriaDbContext context, string propertyName)
    {
        var entityType = context.Model.FindEntityType(typeof(EstablishmentBranch))!;
        return entityType.FindProperty(propertyName)!;
    }
}
