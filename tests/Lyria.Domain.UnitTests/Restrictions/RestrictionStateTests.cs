using Lyria.Domain.Restrictions;
using Xunit;

namespace Lyria.Domain.UnitTests.Restrictions;

public sealed class RestrictionStateTests
{
    private static Restriction CreateRestriction() =>
        Restriction.Create(RestrictionId.New(), "Vegano", null);

    [Fact]
    public void Update_ChangesNameAndDescription()
    {
        var restriction = CreateRestriction();
        restriction.Update("Vegetariano", "Sin carne.");

        Assert.Equal("Vegetariano", restriction.Name);
        Assert.Equal("Sin carne.", restriction.Description);
    }

    [Fact]
    public void Update_TrimsName()
    {
        var restriction = CreateRestriction();
        restriction.Update("  Sin TACC  ", null);
        Assert.Equal("Sin TACC", restriction.Name);
    }

    [Fact]
    public void Update_EmptyDescription_NormalizesToNull()
    {
        var restriction = Restriction.Create(RestrictionId.New(), "Vegano", "Una descripción.");
        restriction.Update("Vegano", "");
        Assert.Null(restriction.Description);
    }

    [Fact]
    public void Update_InvalidName_Throws()
    {
        var restriction = CreateRestriction();
        Assert.Throws<RestrictionException>(() =>
            restriction.Update("", null));
    }

    [Fact]
    public void Update_DoesNotChangeIsActive()
    {
        var restriction = CreateRestriction();
        restriction.Deactivate();
        restriction.Update("Vegetariano", null);
        Assert.False(restriction.IsActive);
    }

    [Fact]
    public void Activate_SetsIsActiveTrue()
    {
        var restriction = CreateRestriction();
        restriction.Deactivate();
        restriction.Activate();
        Assert.True(restriction.IsActive);
    }

    [Fact]
    public void Deactivate_SetsIsActiveFalse()
    {
        var restriction = CreateRestriction();
        restriction.Deactivate();
        Assert.False(restriction.IsActive);
    }

    [Fact]
    public void Activate_WhenAlreadyActive_IsIdempotent()
    {
        var restriction = CreateRestriction();
        Assert.True(restriction.IsActive);
        restriction.Activate();
        Assert.True(restriction.IsActive);
    }

    [Fact]
    public void Deactivate_WhenAlreadyInactive_IsIdempotent()
    {
        var restriction = CreateRestriction();
        restriction.Deactivate();
        Assert.False(restriction.IsActive);
        restriction.Deactivate();
        Assert.False(restriction.IsActive);
    }

    [Fact]
    public void Update_PreservesId()
    {
        var id = RestrictionId.New();
        var restriction = Restriction.Create(id, "Vegano", null);
        restriction.Update("Vegetariano", "Descripción.");
        Assert.Equal(id, restriction.Id);
    }
}
