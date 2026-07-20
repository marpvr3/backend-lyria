using Lyria.Domain.Abstractions;
using Lyria.Domain.Establishments.Branches;
using Lyria.Domain.Restrictions;
using Xunit;

namespace Lyria.Domain.UnitTests.Establishments.Branches;

public sealed class EstablishmentBranchRestrictionTests
{
    private static readonly EstablishmentBranchId BranchId = EstablishmentBranchId.New();
    private static readonly RestrictionId RestrictionId = RestrictionId.New();

    private static EstablishmentBranchRestriction CreateBranchRestriction(
        RestrictionComplianceLevel complianceLevel = RestrictionComplianceLevel.Guaranteed,
        bool isCertified = false,
        string? observation = null)
    {
        return EstablishmentBranchRestriction.Create(
            BranchId, RestrictionId, complianceLevel, isCertified, observation);
    }

    [Fact]
    public void Create_ValidData_SetsAllProperties()
    {
        var branchRestriction = EstablishmentBranchRestriction.Create(
            BranchId, RestrictionId, RestrictionComplianceLevel.Guaranteed,
            true, "La cocina utiliza utensilios separados.");

        Assert.Equal(BranchId, branchRestriction.BranchId);
        Assert.Equal(RestrictionId, branchRestriction.RestrictionId);
        Assert.Equal(RestrictionComplianceLevel.Guaranteed, branchRestriction.ComplianceLevel);
        Assert.True(branchRestriction.IsCertified);
        Assert.Equal("La cocina utiliza utensilios separados.", branchRestriction.Observation);
    }

    [Fact]
    public void Create_NewRelation_StartsActive()
    {
        var branchRestriction = CreateBranchRestriction();
        Assert.True(branchRestriction.IsActive);
    }

    [Fact]
    public void Create_BranchId_IsPreserved()
    {
        var branchRestriction = CreateBranchRestriction();
        Assert.Equal(BranchId, branchRestriction.BranchId);
    }

    [Fact]
    public void Create_RestrictionId_IsPreserved()
    {
        var branchRestriction = CreateBranchRestriction();
        Assert.Equal(RestrictionId, branchRestriction.RestrictionId);
    }

    [Fact]
    public void Create_ComplianceLevelGuaranteed_IsValid()
    {
        var branchRestriction = CreateBranchRestriction(complianceLevel: RestrictionComplianceLevel.Guaranteed);
        Assert.Equal(RestrictionComplianceLevel.Guaranteed, branchRestriction.ComplianceLevel);
    }

    [Fact]
    public void Create_ComplianceLevelPartial_IsValid()
    {
        var branchRestriction = CreateBranchRestriction(complianceLevel: RestrictionComplianceLevel.Partial);
        Assert.Equal(RestrictionComplianceLevel.Partial, branchRestriction.ComplianceLevel);
    }

    [Fact]
    public void Create_ComplianceLevelOnRequest_IsValid()
    {
        var branchRestriction = CreateBranchRestriction(complianceLevel: RestrictionComplianceLevel.OnRequest);
        Assert.Equal(RestrictionComplianceLevel.OnRequest, branchRestriction.ComplianceLevel);
    }

    [Fact]
    public void Create_ComplianceLevelZero_Throws()
    {
        Assert.Throws<EstablishmentBranchException>(() =>
            EstablishmentBranchRestriction.Create(
                BranchId, RestrictionId, (RestrictionComplianceLevel)0, false, null));
    }

    [Fact]
    public void Create_ComplianceLevelOutOfEnum_Throws()
    {
        Assert.Throws<EstablishmentBranchException>(() =>
            EstablishmentBranchRestriction.Create(
                BranchId, RestrictionId, (RestrictionComplianceLevel)99, false, null));
    }

    [Fact]
    public void Create_IsCertifiedTrue_IsPreserved()
    {
        var branchRestriction = CreateBranchRestriction(isCertified: true);
        Assert.True(branchRestriction.IsCertified);
    }

    [Fact]
    public void Create_IsCertifiedFalse_IsPreserved()
    {
        var branchRestriction = CreateBranchRestriction(isCertified: false);
        Assert.False(branchRestriction.IsCertified);
    }

    [Fact]
    public void Create_Guaranteed_DoesNotActivateCertification()
    {
        var branchRestriction = EstablishmentBranchRestriction.Create(
            BranchId, RestrictionId, RestrictionComplianceLevel.Guaranteed, false, null);

        Assert.False(branchRestriction.IsCertified);
    }

    [Fact]
    public void Create_ObservationWithSpaces_AppliesTrim()
    {
        var branchRestriction = EstablishmentBranchRestriction.Create(
            BranchId, RestrictionId, RestrictionComplianceLevel.Guaranteed,
            false, "  Observación con espacios  ");

        Assert.Equal("Observación con espacios", branchRestriction.Observation);
    }

    [Fact]
    public void Create_EmptyObservation_NormalizesToNull()
    {
        var branchRestriction = EstablishmentBranchRestriction.Create(
            BranchId, RestrictionId, RestrictionComplianceLevel.Guaranteed, false, "");

        Assert.Null(branchRestriction.Observation);
    }

    [Fact]
    public void Create_ObservationTooLong_Throws()
    {
        string longObservation = new('A', EstablishmentBranchRestriction.ObservationMaxLength + 1);

        Assert.Throws<EstablishmentBranchException>(() =>
            EstablishmentBranchRestriction.Create(
                BranchId, RestrictionId, RestrictionComplianceLevel.Guaranteed, false, longObservation));
    }

    [Fact]
    public void Update_ChangesComplianceLevel()
    {
        var branchRestriction = CreateBranchRestriction(complianceLevel: RestrictionComplianceLevel.Guaranteed);

        branchRestriction.Update(RestrictionComplianceLevel.Partial, false, null);

        Assert.Equal(RestrictionComplianceLevel.Partial, branchRestriction.ComplianceLevel);
    }

    [Fact]
    public void Update_ChangesIsCertified()
    {
        var branchRestriction = CreateBranchRestriction(isCertified: false);

        branchRestriction.Update(RestrictionComplianceLevel.Guaranteed, true, null);

        Assert.True(branchRestriction.IsCertified);
    }

    [Fact]
    public void Update_ChangesObservation()
    {
        var branchRestriction = CreateBranchRestriction(observation: "Original");

        branchRestriction.Update(RestrictionComplianceLevel.Guaranteed, false, "Nueva observación");

        Assert.Equal("Nueva observación", branchRestriction.Observation);
    }

    [Fact]
    public void Activate_SetsIsActiveTrue()
    {
        var branchRestriction = CreateBranchRestriction();
        branchRestriction.Deactivate();

        branchRestriction.Activate();

        Assert.True(branchRestriction.IsActive);
    }

    [Fact]
    public void Deactivate_SetsIsActiveFalse()
    {
        var branchRestriction = CreateBranchRestriction();

        branchRestriction.Deactivate();

        Assert.False(branchRestriction.IsActive);
    }

    [Fact]
    public void Activate_WhenAlreadyActive_IsIdempotent()
    {
        var branchRestriction = CreateBranchRestriction();
        Assert.True(branchRestriction.IsActive);

        branchRestriction.Activate();

        Assert.True(branchRestriction.IsActive);
    }

    [Fact]
    public void Deactivate_WhenAlreadyInactive_IsIdempotent()
    {
        var branchRestriction = CreateBranchRestriction();
        branchRestriction.Deactivate();

        branchRestriction.Deactivate();

        Assert.False(branchRestriction.IsActive);
    }

    [Fact]
    public void Deactivate_DoesNotModifyComplianceLevel()
    {
        var branchRestriction = CreateBranchRestriction(complianceLevel: RestrictionComplianceLevel.Guaranteed);

        branchRestriction.Deactivate();

        Assert.Equal(RestrictionComplianceLevel.Guaranteed, branchRestriction.ComplianceLevel);
    }

    [Fact]
    public void Deactivate_DoesNotModifyIsCertified()
    {
        var branchRestriction = CreateBranchRestriction(isCertified: true);

        branchRestriction.Deactivate();

        Assert.True(branchRestriction.IsCertified);
    }

    [Fact]
    public void Deactivate_DoesNotModifyObservation()
    {
        var branchRestriction = CreateBranchRestriction(observation: "Observación original");

        branchRestriction.Deactivate();

        Assert.Equal("Observación original", branchRestriction.Observation);
    }

    [Fact]
    public void BranchId_DoesNotChange_AfterCreate()
    {
        var branchRestriction = CreateBranchRestriction();
        var originalBranchId = branchRestriction.BranchId;

        branchRestriction.Update(RestrictionComplianceLevel.Partial, true, "Cambio");
        branchRestriction.Activate();
        branchRestriction.Deactivate();

        Assert.Equal(originalBranchId, branchRestriction.BranchId);
    }

    [Fact]
    public void RestrictionId_DoesNotChange_AfterCreate()
    {
        var branchRestriction = CreateBranchRestriction();
        var originalRestrictionId = branchRestriction.RestrictionId;

        branchRestriction.Update(RestrictionComplianceLevel.Partial, true, "Cambio");
        branchRestriction.Activate();
        branchRestriction.Deactivate();

        Assert.Equal(originalRestrictionId, branchRestriction.RestrictionId);
    }

    [Fact]
    public void NoPublicSetters()
    {
        var properties = typeof(EstablishmentBranchRestriction).GetProperties();
        foreach (var prop in properties)
        {
            var setter = prop.GetSetMethod(nonPublic: false);
            Assert.Null(setter);
        }
    }

    [Fact]
    public void ImplementsIAuditableEntity()
    {
        Assert.True(typeof(IAuditableEntity).IsAssignableFrom(typeof(EstablishmentBranchRestriction)));
    }
}
