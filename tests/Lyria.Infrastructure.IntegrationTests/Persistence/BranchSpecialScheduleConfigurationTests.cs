using Lyria.Domain.Establishments.Branches;
using Lyria.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Lyria.Infrastructure.IntegrationTests.Persistence;

public sealed class BranchSpecialScheduleConfigurationTests : IDisposable
{
    private readonly SqliteFixture _fixture = new();

    [Fact]
    public void TableName_Should_Be_HorariosEspecialesSede()
    {
        using var context = _fixture.CreateContext();
        var entityType = context.Model.FindEntityType(typeof(BranchSpecialSchedule))!;
        Assert.Equal("HorariosEspecialesSede", entityType.GetTableName());
    }

    [Fact]
    public void PrimaryKey_Should_Be_Named_PK_HorariosEspecialesSede()
    {
        using var context = _fixture.CreateContext();
        var entityType = context.Model.FindEntityType(typeof(BranchSpecialSchedule))!;
        var primaryKey = entityType.FindPrimaryKey()!;
        Assert.Equal("PK_HorariosEspecialesSede", primaryKey.GetName());
    }

    [Fact]
    public void Id_Column_Should_Be_HorarioEspecialSedeId()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(BranchSpecialSchedule.Id));
        Assert.Equal("HorarioEspecialSedeId", property.GetColumnName());
    }

    [Fact]
    public void Id_Should_Have_ValueConverter()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(BranchSpecialSchedule.Id));
        var converter = property.GetValueConverter();
        Assert.NotNull(converter);
        Assert.Equal(typeof(BranchSpecialScheduleId), converter.ModelClrType);
        Assert.Equal(typeof(Guid), converter.ProviderClrType);
    }

    [Fact]
    public void Id_Should_Be_ValueGeneratedNever()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(BranchSpecialSchedule.Id));
        Assert.Equal(ValueGenerated.Never, property.ValueGenerated);
    }

    [Fact]
    public void BranchId_Column_Should_Be_SedeId()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(BranchSpecialSchedule.BranchId));
        Assert.Equal("SedeId", property.GetColumnName());
    }

    [Fact]
    public void BranchId_Should_Be_Required()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(BranchSpecialSchedule.BranchId));
        Assert.False(property.IsNullable);
    }

    [Fact]
    public void BranchId_Should_Have_ValueConverter()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(BranchSpecialSchedule.BranchId));
        var converter = property.GetValueConverter();
        Assert.NotNull(converter);
        Assert.Equal(typeof(EstablishmentBranchId), converter.ModelClrType);
        Assert.Equal(typeof(Guid), converter.ProviderClrType);
    }

    [Fact]
    public void Date_Column_Should_Be_Fecha()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(BranchSpecialSchedule.Date));
        Assert.Equal("Fecha", property.GetColumnName());
    }

    [Fact]
    public void Date_Should_Be_Required()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(BranchSpecialSchedule.Date));
        Assert.False(property.IsNullable);
    }

    [Fact]
    public void OpeningTime_Column_Should_Be_HoraApertura()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(BranchSpecialSchedule.OpeningTime));
        Assert.Equal("HoraApertura", property.GetColumnName());
    }

    [Fact]
    public void OpeningTime_Should_Be_Optional()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(BranchSpecialSchedule.OpeningTime));
        Assert.True(property.IsNullable);
    }

    [Fact]
    public void ClosingTime_Column_Should_Be_HoraCierre()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(BranchSpecialSchedule.ClosingTime));
        Assert.Equal("HoraCierre", property.GetColumnName());
    }

    [Fact]
    public void ClosingTime_Should_Be_Optional()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(BranchSpecialSchedule.ClosingTime));
        Assert.True(property.IsNullable);
    }

    [Fact]
    public void CrossesMidnight_Column_Should_Be_CruzaMedianoche()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(BranchSpecialSchedule.CrossesMidnight));
        Assert.Equal("CruzaMedianoche", property.GetColumnName());
    }

    [Fact]
    public void CrossesMidnight_Should_Be_Required()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(BranchSpecialSchedule.CrossesMidnight));
        Assert.False(property.IsNullable);
    }

    [Fact]
    public void IsClosed_Column_Should_Be_Cerrado()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(BranchSpecialSchedule.IsClosed));
        Assert.Equal("Cerrado", property.GetColumnName());
    }

    [Fact]
    public void IsClosed_Should_Be_Required()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(BranchSpecialSchedule.IsClosed));
        Assert.False(property.IsNullable);
    }

    [Fact]
    public void Reason_Column_Should_Be_Motivo()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(BranchSpecialSchedule.Reason));
        Assert.Equal("Motivo", property.GetColumnName());
    }

    [Fact]
    public void Reason_Should_Be_Optional()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(BranchSpecialSchedule.Reason));
        Assert.True(property.IsNullable);
    }

    [Fact]
    public void Reason_Should_Have_MaxLength()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(BranchSpecialSchedule.Reason));
        Assert.Equal(BranchSpecialSchedule.ReasonMaxLength, property.GetMaxLength());
    }

    [Fact]
    public void IsActive_Column_Should_Be_EsActivo()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(BranchSpecialSchedule.IsActive));
        Assert.Equal("EsActivo", property.GetColumnName());
    }

    [Fact]
    public void IsActive_Should_Be_Required()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(BranchSpecialSchedule.IsActive));
        Assert.False(property.IsNullable);
    }

    [Fact]
    public void CreatedAtUtc_Column_Should_Be_FechaCreacionUtc()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(BranchSpecialSchedule.CreatedAtUtc));
        Assert.Equal("FechaCreacionUtc", property.GetColumnName());
    }

    [Fact]
    public void CreatedAtUtc_Should_Be_Required()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(BranchSpecialSchedule.CreatedAtUtc));
        Assert.False(property.IsNullable);
    }

    [Fact]
    public void UpdatedAtUtc_Column_Should_Be_FechaActualizacionUtc()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(BranchSpecialSchedule.UpdatedAtUtc));
        Assert.Equal("FechaActualizacionUtc", property.GetColumnName());
    }

    [Fact]
    public void UpdatedAtUtc_Should_Be_Optional()
    {
        using var context = _fixture.CreateContext();
        var property = GetProperty(context, nameof(BranchSpecialSchedule.UpdatedAtUtc));
        Assert.True(property.IsNullable);
    }

    [Fact]
    public void FK_Should_Reference_EstablishmentBranch_With_Restrict()
    {
        using var context = _fixture.CreateContext();
        var entityType = context.Model.FindEntityType(typeof(BranchSpecialSchedule))!;
        var foreignKey = entityType.GetForeignKeys().First();
        Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior);
        Assert.Equal("FK_HorariosEspecialesSede_Sedes_SedeId", foreignKey.GetConstraintName());
    }

    [Fact]
    public void Index_On_BranchId_Should_Exist()
    {
        using var context = _fixture.CreateContext();
        var entityType = context.Model.FindEntityType(typeof(BranchSpecialSchedule))!;
        var index = entityType.GetIndexes()
            .FirstOrDefault(i => i.Properties.Count == 1
                && i.Properties[0].Name == nameof(BranchSpecialSchedule.BranchId));
        Assert.NotNull(index);
        Assert.Equal("IX_HorariosEspecialesSede_SedeId", index.GetDatabaseName());
    }

    [Fact]
    public void Index_On_BranchId_Date_Should_Exist()
    {
        using var context = _fixture.CreateContext();
        var entityType = context.Model.FindEntityType(typeof(BranchSpecialSchedule))!;
        var index = entityType.GetIndexes()
            .FirstOrDefault(i => i.Properties.Count == 2
                && i.Properties[0].Name == nameof(BranchSpecialSchedule.BranchId)
                && i.Properties[1].Name == nameof(BranchSpecialSchedule.Date));
        Assert.NotNull(index);
        Assert.Equal("IX_HorariosEspecialesSede_SedeId_Fecha", index.GetDatabaseName());
    }

    [Fact]
    public void Index_On_BranchId_Date_IsActive_Should_Exist()
    {
        using var context = _fixture.CreateContext();
        var entityType = context.Model.FindEntityType(typeof(BranchSpecialSchedule))!;
        var index = entityType.GetIndexes()
            .FirstOrDefault(i => i.Properties.Count == 3
                && i.Properties[0].Name == nameof(BranchSpecialSchedule.BranchId)
                && i.Properties[1].Name == nameof(BranchSpecialSchedule.Date)
                && i.Properties[2].Name == nameof(BranchSpecialSchedule.IsActive));
        Assert.NotNull(index);
        Assert.Equal("IX_HorariosEspecialesSede_SedeId_Fecha_EsActivo", index.GetDatabaseName());
    }

    public void Dispose() => _fixture.Dispose();

    private static IProperty GetProperty(LyriaDbContext context, string propertyName)
    {
        var entityType = context.Model.FindEntityType(typeof(BranchSpecialSchedule))!;
        return entityType.FindProperty(propertyName)!;
    }
}
