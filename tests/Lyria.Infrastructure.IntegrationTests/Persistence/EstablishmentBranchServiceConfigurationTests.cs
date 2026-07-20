using Lyria.Domain.Establishments.Branches;
using Lyria.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Lyria.Infrastructure.IntegrationTests.Persistence;

public sealed class EstablishmentBranchServiceConfigurationTests : IDisposable
{
    private readonly SqliteFixture _fixture = new();

    [Fact]
    public void Table_Name_Is_SedesServicios()
    {
        using var context = _fixture.CreateContext();
        var entityType = context.Model.FindEntityType(typeof(EstablishmentBranchService))!;
        Assert.Equal("SedesServicios", entityType.GetTableName());
    }

    [Fact]
    public void PrimaryKey_Is_Composite_SedeId_ServicioId()
    {
        using var context = _fixture.CreateContext();
        var entityType = context.Model.FindEntityType(typeof(EstablishmentBranchService))!;
        var pk = entityType.FindPrimaryKey()!;
        var columns = pk.Properties.Select(p => p.GetColumnName()).ToList();
        Assert.Equal(2, columns.Count);
        Assert.Contains("SedeId", columns);
        Assert.Contains("ServicioId", columns);
    }

    [Fact]
    public void PrimaryKey_Name_Is_PK_SedesServicios()
    {
        using var context = _fixture.CreateContext();
        var entityType = context.Model.FindEntityType(typeof(EstablishmentBranchService))!;
        var pk = entityType.FindPrimaryKey()!;
        Assert.Equal("PK_SedesServicios", pk.GetName());
    }

    [Fact]
    public void Column_BranchId_Name_Is_SedeId()
    {
        using var context = _fixture.CreateContext();
        var property = context.Model.FindEntityType(typeof(EstablishmentBranchService))!
            .FindProperty(nameof(EstablishmentBranchService.BranchId))!;
        Assert.Equal("SedeId", property.GetColumnName());
    }

    [Fact]
    public void Column_ServiceId_Name_Is_ServicioId()
    {
        using var context = _fixture.CreateContext();
        var property = context.Model.FindEntityType(typeof(EstablishmentBranchService))!
            .FindProperty(nameof(EstablishmentBranchService.ServiceId))!;
        Assert.Equal("ServicioId", property.GetColumnName());
    }

    [Fact]
    public void Column_IsAvailable_Name_Is_Disponible()
    {
        using var context = _fixture.CreateContext();
        var property = context.Model.FindEntityType(typeof(EstablishmentBranchService))!
            .FindProperty(nameof(EstablishmentBranchService.IsAvailable))!;
        Assert.Equal("Disponible", property.GetColumnName());
    }

    [Fact]
    public void Column_IsAvailable_IsRequired()
    {
        using var context = _fixture.CreateContext();
        var property = context.Model.FindEntityType(typeof(EstablishmentBranchService))!
            .FindProperty(nameof(EstablishmentBranchService.IsAvailable))!;
        Assert.False(property.IsNullable);
    }

    [Fact]
    public void Column_Observation_Name_Is_Observacion()
    {
        using var context = _fixture.CreateContext();
        var property = context.Model.FindEntityType(typeof(EstablishmentBranchService))!
            .FindProperty(nameof(EstablishmentBranchService.Observation))!;
        Assert.Equal("Observacion", property.GetColumnName());
    }

    [Fact]
    public void Column_Observation_MaxLength_Is_500()
    {
        using var context = _fixture.CreateContext();
        var property = context.Model.FindEntityType(typeof(EstablishmentBranchService))!
            .FindProperty(nameof(EstablishmentBranchService.Observation))!;
        Assert.Equal(500, property.GetMaxLength());
    }

    [Fact]
    public void Column_Observation_IsOptional()
    {
        using var context = _fixture.CreateContext();
        var property = context.Model.FindEntityType(typeof(EstablishmentBranchService))!
            .FindProperty(nameof(EstablishmentBranchService.Observation))!;
        Assert.True(property.IsNullable);
    }

    [Fact]
    public void Column_IsActive_Name_Is_Activo()
    {
        using var context = _fixture.CreateContext();
        var property = context.Model.FindEntityType(typeof(EstablishmentBranchService))!
            .FindProperty(nameof(EstablishmentBranchService.IsActive))!;
        Assert.Equal("Activo", property.GetColumnName());
    }

    [Fact]
    public void Column_IsActive_IsRequired()
    {
        using var context = _fixture.CreateContext();
        var property = context.Model.FindEntityType(typeof(EstablishmentBranchService))!
            .FindProperty(nameof(EstablishmentBranchService.IsActive))!;
        Assert.False(property.IsNullable);
    }

    [Fact]
    public void Column_CreatedAtUtc_Name_Is_FechaCreacion()
    {
        using var context = _fixture.CreateContext();
        var property = context.Model.FindEntityType(typeof(EstablishmentBranchService))!
            .FindProperty(nameof(EstablishmentBranchService.CreatedAtUtc))!;
        Assert.Equal("FechaCreacion", property.GetColumnName());
    }

    [Fact]
    public void Column_UpdatedAtUtc_Name_Is_FechaActualizacion()
    {
        using var context = _fixture.CreateContext();
        var property = context.Model.FindEntityType(typeof(EstablishmentBranchService))!
            .FindProperty(nameof(EstablishmentBranchService.UpdatedAtUtc))!;
        Assert.Equal("FechaActualizacion", property.GetColumnName());
    }

    [Fact]
    public void FK_To_Sedes_IsRestrict()
    {
        using var context = _fixture.CreateContext();
        var entityType = context.Model.FindEntityType(typeof(EstablishmentBranchService))!;
        var fk = entityType.GetForeignKeys()
            .First(f => f.Properties.Any(p => p.GetColumnName() == "SedeId"));
        Assert.Equal(DeleteBehavior.Restrict, fk.DeleteBehavior);
    }

    [Fact]
    public void FK_To_Servicios_IsRestrict()
    {
        using var context = _fixture.CreateContext();
        var entityType = context.Model.FindEntityType(typeof(EstablishmentBranchService))!;
        var fk = entityType.GetForeignKeys()
            .First(f => f.Properties.Any(p => p.GetColumnName() == "ServicioId"));
        Assert.Equal(DeleteBehavior.Restrict, fk.DeleteBehavior);
    }

    [Fact]
    public void HasIndex_On_ServicioId()
    {
        using var context = _fixture.CreateContext();
        var entityType = context.Model.FindEntityType(typeof(EstablishmentBranchService))!;
        var serviceIdProperty = entityType.FindProperty(nameof(EstablishmentBranchService.ServiceId))!;
        var index = entityType.GetIndexes().FirstOrDefault(i =>
            i.Properties.Count == 1 && i.Properties[0] == serviceIdProperty);
        Assert.NotNull(index);
    }

    [Fact]
    public void HasIndex_On_SedeId_Activo()
    {
        using var context = _fixture.CreateContext();
        var entityType = context.Model.FindEntityType(typeof(EstablishmentBranchService))!;
        var index = entityType.GetIndexes().FirstOrDefault(i =>
            i.Properties.Count == 2 &&
            i.Properties[0].GetColumnName() == "SedeId" &&
            i.Properties[1].GetColumnName() == "Activo");
        Assert.NotNull(index);
    }

    [Fact]
    public void HasIndex_On_ServicioId_Activo()
    {
        using var context = _fixture.CreateContext();
        var entityType = context.Model.FindEntityType(typeof(EstablishmentBranchService))!;
        var index = entityType.GetIndexes().FirstOrDefault(i =>
            i.Properties.Count == 2 &&
            i.Properties[0].GetColumnName() == "ServicioId" &&
            i.Properties[1].GetColumnName() == "Activo");
        Assert.NotNull(index);
    }

    public void Dispose() => _fixture.Dispose();
}
