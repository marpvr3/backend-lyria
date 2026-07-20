using Lyria.Domain.Restrictions;
using Lyria.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Lyria.Infrastructure.IntegrationTests.Persistence;

public sealed class RestrictionConfigurationTests : IDisposable
{
    private readonly SqliteFixture _fixture = new();

    [Fact]
    public void Table_Name_Is_Restricciones()
    {
        using var context = _fixture.CreateContext();
        var entityType = context.Model.FindEntityType(typeof(Restriction))!;
        Assert.Equal("Restricciones", entityType.GetTableName());
    }

    [Fact]
    public void Table_Has_No_Schema()
    {
        using var context = _fixture.CreateContext();
        var entityType = context.Model.FindEntityType(typeof(Restriction))!;
        Assert.Null(entityType.GetSchema());
    }

    [Fact]
    public void Column_Id_Name_Is_RestriccionId()
    {
        using var context = _fixture.CreateContext();
        var property = context.Model.FindEntityType(typeof(Restriction))!.FindProperty(nameof(Restriction.Id))!;
        Assert.Equal("RestriccionId", property.GetColumnName());
    }

    [Fact]
    public void Column_Name_Is_Nombre()
    {
        using var context = _fixture.CreateContext();
        var property = context.Model.FindEntityType(typeof(Restriction))!.FindProperty(nameof(Restriction.Name))!;
        Assert.Equal("Nombre", property.GetColumnName());
    }

    [Fact]
    public void Column_Name_MaxLength_Is_100()
    {
        using var context = _fixture.CreateContext();
        var property = context.Model.FindEntityType(typeof(Restriction))!.FindProperty(nameof(Restriction.Name))!;
        Assert.Equal(100, property.GetMaxLength());
    }

    [Fact]
    public void Column_Name_IsRequired()
    {
        using var context = _fixture.CreateContext();
        var property = context.Model.FindEntityType(typeof(Restriction))!.FindProperty(nameof(Restriction.Name))!;
        Assert.False(property.IsNullable);
    }

    [Fact]
    public void Column_Description_Name_Is_Descripcion()
    {
        using var context = _fixture.CreateContext();
        var property = context.Model.FindEntityType(typeof(Restriction))!.FindProperty(nameof(Restriction.Description))!;
        Assert.Equal("Descripcion", property.GetColumnName());
    }

    [Fact]
    public void Column_Description_MaxLength_Is_500()
    {
        using var context = _fixture.CreateContext();
        var property = context.Model.FindEntityType(typeof(Restriction))!.FindProperty(nameof(Restriction.Description))!;
        Assert.Equal(500, property.GetMaxLength());
    }

    [Fact]
    public void Column_Description_IsOptional()
    {
        using var context = _fixture.CreateContext();
        var property = context.Model.FindEntityType(typeof(Restriction))!.FindProperty(nameof(Restriction.Description))!;
        Assert.True(property.IsNullable);
    }

    [Fact]
    public void Column_IsActive_Name_Is_Activo()
    {
        using var context = _fixture.CreateContext();
        var property = context.Model.FindEntityType(typeof(Restriction))!.FindProperty(nameof(Restriction.IsActive))!;
        Assert.Equal("Activo", property.GetColumnName());
    }

    [Fact]
    public void Column_IsActive_IsRequired()
    {
        using var context = _fixture.CreateContext();
        var property = context.Model.FindEntityType(typeof(Restriction))!.FindProperty(nameof(Restriction.IsActive))!;
        Assert.False(property.IsNullable);
    }

    [Fact]
    public void Column_CreatedAtUtc_Name_Is_FechaCreacion()
    {
        using var context = _fixture.CreateContext();
        var property = context.Model.FindEntityType(typeof(Restriction))!.FindProperty(nameof(Restriction.CreatedAtUtc))!;
        Assert.Equal("FechaCreacion", property.GetColumnName());
    }

    [Fact]
    public void Column_UpdatedAtUtc_Name_Is_FechaActualizacion()
    {
        using var context = _fixture.CreateContext();
        var property = context.Model.FindEntityType(typeof(Restriction))!.FindProperty(nameof(Restriction.UpdatedAtUtc))!;
        Assert.Equal("FechaActualizacion", property.GetColumnName());
    }

    [Fact]
    public void PrimaryKey_Name_Is_PK_Restricciones()
    {
        using var context = _fixture.CreateContext();
        var entityType = context.Model.FindEntityType(typeof(Restriction))!;
        var pk = entityType.FindPrimaryKey()!;
        Assert.Equal("PK_Restricciones", pk.GetName());
    }

    [Fact]
    public void Id_HasValueConverter()
    {
        using var context = _fixture.CreateContext();
        var property = context.Model.FindEntityType(typeof(Restriction))!.FindProperty(nameof(Restriction.Id))!;
        Assert.NotNull(property.GetValueConverter());
    }

    [Fact]
    public void Id_ValueGenerated_IsNever()
    {
        using var context = _fixture.CreateContext();
        var property = context.Model.FindEntityType(typeof(Restriction))!.FindProperty(nameof(Restriction.Id))!;
        Assert.Equal(ValueGenerated.Never, property.ValueGenerated);
    }

    [Fact]
    public void DomainEvents_IsNotMapped()
    {
        using var context = _fixture.CreateContext();
        var entityType = context.Model.FindEntityType(typeof(Restriction))!;
        Assert.Null(entityType.FindProperty("DomainEvents"));
    }

    [Fact]
    public void HasUniqueIndex_On_Name()
    {
        using var context = _fixture.CreateContext();
        var entityType = context.Model.FindEntityType(typeof(Restriction))!;
        var nameProperty = entityType.FindProperty(nameof(Restriction.Name))!;
        var index = entityType.GetIndexes().FirstOrDefault(i =>
            i.Properties.Count == 1 && i.Properties[0] == nameProperty && i.IsUnique);
        Assert.NotNull(index);
    }

    [Fact]
    public void HasIndex_On_IsActive()
    {
        using var context = _fixture.CreateContext();
        var entityType = context.Model.FindEntityType(typeof(Restriction))!;
        var activeProperty = entityType.FindProperty(nameof(Restriction.IsActive))!;
        var index = entityType.GetIndexes().FirstOrDefault(i =>
            i.Properties.Count == 1 && i.Properties[0] == activeProperty);
        Assert.NotNull(index);
    }

    public void Dispose() => _fixture.Dispose();
}
