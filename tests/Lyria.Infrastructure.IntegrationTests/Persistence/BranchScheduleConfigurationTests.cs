using Lyria.Domain.Establishments.Branches;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Lyria.Infrastructure.IntegrationTests.Persistence;

public sealed class BranchScheduleConfigurationTests : IDisposable
{
    private readonly SqliteFixture _fixture = new();

    [Fact]
    public void Table_Name_Is_HorariosSede()
    {
        using var context = _fixture.CreateContext();
        var entityType = context.Model.FindEntityType(typeof(BranchSchedule))!;
        Assert.Equal("HorariosSede", entityType.GetTableName());
    }

    [Fact]
    public void PrimaryKey_Name_Is_PK_HorariosSede()
    {
        using var context = _fixture.CreateContext();
        var entityType = context.Model.FindEntityType(typeof(BranchSchedule))!;
        var pk = entityType.FindPrimaryKey()!;
        Assert.Equal("PK_HorariosSede", pk.GetName());
    }

    [Fact]
    public void PrimaryKey_Column_Is_HorarioSedeId()
    {
        using var context = _fixture.CreateContext();
        var entityType = context.Model.FindEntityType(typeof(BranchSchedule))!;
        var pk = entityType.FindPrimaryKey()!;
        var column = pk.Properties.Single().GetColumnName();
        Assert.Equal("HorarioSedeId", column);
    }

    [Fact]
    public void Column_BranchId_Name_Is_SedeId()
    {
        using var context = _fixture.CreateContext();
        var property = context.Model.FindEntityType(typeof(BranchSchedule))!
            .FindProperty(nameof(BranchSchedule.BranchId))!;
        Assert.Equal("SedeId", property.GetColumnName());
    }

    [Fact]
    public void Column_DayOfWeek_Name_Is_DiaSemana()
    {
        using var context = _fixture.CreateContext();
        var property = context.Model.FindEntityType(typeof(BranchSchedule))!
            .FindProperty(nameof(BranchSchedule.DayOfWeek))!;
        Assert.Equal("DiaSemana", property.GetColumnName());
    }

    [Fact]
    public void Column_OpeningTime_Name_Is_HoraApertura()
    {
        using var context = _fixture.CreateContext();
        var property = context.Model.FindEntityType(typeof(BranchSchedule))!
            .FindProperty(nameof(BranchSchedule.OpeningTime))!;
        Assert.Equal("HoraApertura", property.GetColumnName());
    }

    [Fact]
    public void Column_OpeningTime_IsOptional()
    {
        using var context = _fixture.CreateContext();
        var property = context.Model.FindEntityType(typeof(BranchSchedule))!
            .FindProperty(nameof(BranchSchedule.OpeningTime))!;
        Assert.True(property.IsNullable);
    }

    [Fact]
    public void Column_ClosingTime_Name_Is_HoraCierre()
    {
        using var context = _fixture.CreateContext();
        var property = context.Model.FindEntityType(typeof(BranchSchedule))!
            .FindProperty(nameof(BranchSchedule.ClosingTime))!;
        Assert.Equal("HoraCierre", property.GetColumnName());
    }

    [Fact]
    public void Column_ClosingTime_IsOptional()
    {
        using var context = _fixture.CreateContext();
        var property = context.Model.FindEntityType(typeof(BranchSchedule))!
            .FindProperty(nameof(BranchSchedule.ClosingTime))!;
        Assert.True(property.IsNullable);
    }

    [Fact]
    public void Column_CrossesMidnight_Name_Is_CruzaMedianoche()
    {
        using var context = _fixture.CreateContext();
        var property = context.Model.FindEntityType(typeof(BranchSchedule))!
            .FindProperty(nameof(BranchSchedule.CrossesMidnight))!;
        Assert.Equal("CruzaMedianoche", property.GetColumnName());
    }

    [Fact]
    public void Column_CrossesMidnight_IsRequired()
    {
        using var context = _fixture.CreateContext();
        var property = context.Model.FindEntityType(typeof(BranchSchedule))!
            .FindProperty(nameof(BranchSchedule.CrossesMidnight))!;
        Assert.False(property.IsNullable);
    }

    [Fact]
    public void Column_IsClosed_Name_Is_Cerrado()
    {
        using var context = _fixture.CreateContext();
        var property = context.Model.FindEntityType(typeof(BranchSchedule))!
            .FindProperty(nameof(BranchSchedule.IsClosed))!;
        Assert.Equal("Cerrado", property.GetColumnName());
    }

    [Fact]
    public void Column_IsActive_Name_Is_EsActivo()
    {
        using var context = _fixture.CreateContext();
        var property = context.Model.FindEntityType(typeof(BranchSchedule))!
            .FindProperty(nameof(BranchSchedule.IsActive))!;
        Assert.Equal("EsActivo", property.GetColumnName());
    }

    [Fact]
    public void Column_CreatedAtUtc_Name_Is_FechaCreacionUtc()
    {
        using var context = _fixture.CreateContext();
        var property = context.Model.FindEntityType(typeof(BranchSchedule))!
            .FindProperty(nameof(BranchSchedule.CreatedAtUtc))!;
        Assert.Equal("FechaCreacionUtc", property.GetColumnName());
    }

    [Fact]
    public void Column_UpdatedAtUtc_Name_Is_FechaActualizacionUtc()
    {
        using var context = _fixture.CreateContext();
        var property = context.Model.FindEntityType(typeof(BranchSchedule))!
            .FindProperty(nameof(BranchSchedule.UpdatedAtUtc))!;
        Assert.Equal("FechaActualizacionUtc", property.GetColumnName());
    }

    [Fact]
    public void FK_To_Sedes_IsRestrict()
    {
        using var context = _fixture.CreateContext();
        var entityType = context.Model.FindEntityType(typeof(BranchSchedule))!;
        var fk = entityType.GetForeignKeys()
            .First(f => f.Properties.Any(p => p.GetColumnName() == "SedeId"));
        Assert.Equal(DeleteBehavior.Restrict, fk.DeleteBehavior);
    }

    [Fact]
    public void HasIndex_On_SedeId()
    {
        using var context = _fixture.CreateContext();
        var entityType = context.Model.FindEntityType(typeof(BranchSchedule))!;
        var property = entityType.FindProperty(nameof(BranchSchedule.BranchId))!;
        var index = entityType.GetIndexes().FirstOrDefault(i =>
            i.Properties.Count == 1 && i.Properties[0] == property);
        Assert.NotNull(index);
    }

    [Fact]
    public void HasIndex_On_SedeId_DiaSemana()
    {
        using var context = _fixture.CreateContext();
        var entityType = context.Model.FindEntityType(typeof(BranchSchedule))!;
        var index = entityType.GetIndexes().FirstOrDefault(i =>
            i.Properties.Count == 2 &&
            i.Properties[0].GetColumnName() == "SedeId" &&
            i.Properties[1].GetColumnName() == "DiaSemana");
        Assert.NotNull(index);
    }

    [Fact]
    public void HasIndex_On_SedeId_DiaSemana_EsActivo()
    {
        using var context = _fixture.CreateContext();
        var entityType = context.Model.FindEntityType(typeof(BranchSchedule))!;
        var index = entityType.GetIndexes().FirstOrDefault(i =>
            i.Properties.Count == 3 &&
            i.Properties[0].GetColumnName() == "SedeId" &&
            i.Properties[1].GetColumnName() == "DiaSemana" &&
            i.Properties[2].GetColumnName() == "EsActivo");
        Assert.NotNull(index);
    }

    public void Dispose() => _fixture.Dispose();
}
