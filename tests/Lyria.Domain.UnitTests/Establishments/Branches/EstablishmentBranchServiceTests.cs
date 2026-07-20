using Lyria.Domain.Abstractions;
using Lyria.Domain.Establishments.Branches;
using Lyria.Domain.Services;
using Xunit;

namespace Lyria.Domain.UnitTests.Establishments.Branches;

public sealed class EstablishmentBranchServiceTests
{
    private static readonly EstablishmentBranchId BranchId = EstablishmentBranchId.New();
    private static readonly ServiceId ServiceId = ServiceId.New();

    private static EstablishmentBranchService CreateBranchService(
        bool isAvailable = true,
        string? observation = null)
    {
        return EstablishmentBranchService.Create(BranchId, ServiceId, isAvailable, observation);
    }

    [Fact]
    public void Create_ValidData_SetsAllProperties()
    {
        var branchService = EstablishmentBranchService.Create(
            BranchId, ServiceId, true, "Disponible dentro de un radio de cinco kilómetros.");

        Assert.Equal(BranchId, branchService.BranchId);
        Assert.Equal(ServiceId, branchService.ServiceId);
        Assert.True(branchService.IsAvailable);
        Assert.Equal("Disponible dentro de un radio de cinco kilómetros.", branchService.Observation);
    }

    [Fact]
    public void Create_NewRelation_StartsActive()
    {
        var branchService = CreateBranchService();
        Assert.True(branchService.IsActive);
    }

    [Fact]
    public void Create_IsAvailableTrue_SetsAvailable()
    {
        var branchService = CreateBranchService(isAvailable: true);
        Assert.True(branchService.IsAvailable);
    }

    [Fact]
    public void Create_IsAvailableFalse_SetsNotAvailable()
    {
        var branchService = CreateBranchService(isAvailable: false);
        Assert.False(branchService.IsAvailable);
    }

    [Fact]
    public void Create_ObservationWithSpaces_AppliesTrim()
    {
        var branchService = EstablishmentBranchService.Create(
            BranchId, ServiceId, true, "  Observación con espacios  ");

        Assert.Equal("Observación con espacios", branchService.Observation);
    }

    [Fact]
    public void Create_EmptyObservation_NormalizesToNull()
    {
        var branchService = EstablishmentBranchService.Create(
            BranchId, ServiceId, true, "");

        Assert.Null(branchService.Observation);
    }

    [Fact]
    public void Create_WhitespaceObservation_NormalizesToNull()
    {
        var branchService = EstablishmentBranchService.Create(
            BranchId, ServiceId, true, "   ");

        Assert.Null(branchService.Observation);
    }

    [Fact]
    public void Create_ObservationTooLong_Throws()
    {
        string longObservation = new('A', EstablishmentBranchService.ObservationMaxLength + 1);

        Assert.Throws<EstablishmentBranchException>(() =>
            EstablishmentBranchService.Create(BranchId, ServiceId, true, longObservation));
    }

    [Fact]
    public void Update_ChangesAvailability()
    {
        var branchService = CreateBranchService(isAvailable: true);

        branchService.Update(false, null);

        Assert.False(branchService.IsAvailable);
    }

    [Fact]
    public void Update_ChangesObservation()
    {
        var branchService = CreateBranchService(observation: "Original");

        branchService.Update(true, "Nueva observación");

        Assert.Equal("Nueva observación", branchService.Observation);
    }

    [Fact]
    public void Update_ObservationTooLong_Throws()
    {
        var branchService = CreateBranchService();
        string longObservation = new('A', EstablishmentBranchService.ObservationMaxLength + 1);

        Assert.Throws<EstablishmentBranchException>(() =>
            branchService.Update(true, longObservation));
    }

    [Fact]
    public void Activate_SetsIsActiveTrue()
    {
        var branchService = CreateBranchService();
        branchService.Deactivate();

        branchService.Activate();

        Assert.True(branchService.IsActive);
    }

    [Fact]
    public void Deactivate_SetsIsActiveFalse()
    {
        var branchService = CreateBranchService();

        branchService.Deactivate();

        Assert.False(branchService.IsActive);
    }

    [Fact]
    public void Activate_WhenAlreadyActive_IsIdempotent()
    {
        var branchService = CreateBranchService();
        Assert.True(branchService.IsActive);

        branchService.Activate();

        Assert.True(branchService.IsActive);
    }

    [Fact]
    public void Deactivate_WhenAlreadyInactive_IsIdempotent()
    {
        var branchService = CreateBranchService();
        branchService.Deactivate();

        branchService.Deactivate();

        Assert.False(branchService.IsActive);
    }

    [Fact]
    public void BranchId_DoesNotChange_AfterCreate()
    {
        var branchService = CreateBranchService();
        var originalBranchId = branchService.BranchId;

        branchService.Update(false, "Cambio");
        branchService.Activate();
        branchService.Deactivate();

        Assert.Equal(originalBranchId, branchService.BranchId);
    }

    [Fact]
    public void ServiceId_DoesNotChange_AfterCreate()
    {
        var branchService = CreateBranchService();
        var originalServiceId = branchService.ServiceId;

        branchService.Update(false, "Cambio");
        branchService.Activate();
        branchService.Deactivate();

        Assert.Equal(originalServiceId, branchService.ServiceId);
    }

    [Fact]
    public void NoPublicSetters()
    {
        var properties = typeof(EstablishmentBranchService).GetProperties();
        foreach (var prop in properties)
        {
            var setter = prop.GetSetMethod(nonPublic: false);
            Assert.Null(setter);
        }
    }

    [Fact]
    public void ImplementsIAuditableEntity()
    {
        Assert.True(typeof(IAuditableEntity).IsAssignableFrom(typeof(EstablishmentBranchService)));
    }
}
