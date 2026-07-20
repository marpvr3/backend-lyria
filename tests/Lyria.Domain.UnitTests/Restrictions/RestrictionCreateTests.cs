using Lyria.Domain.Restrictions;
using Xunit;

namespace Lyria.Domain.UnitTests.Restrictions;

public sealed class RestrictionCreateTests
{
    [Fact]
    public void Create_ValidData_SetsAllProperties()
    {
        var id = RestrictionId.New();
        var restriction = Restriction.Create(id, "Vegano", "Sin productos de origen animal.");

        Assert.Equal(id, restriction.Id);
        Assert.Equal("Vegano", restriction.Name);
        Assert.Equal("Sin productos de origen animal.", restriction.Description);
    }

    [Fact]
    public void Create_NewRestriction_StartsActive()
    {
        var restriction = Restriction.Create(RestrictionId.New(), "Vegano", null);
        Assert.True(restriction.IsActive);
    }

    [Fact]
    public void Create_NameWithSpaces_AppliesTrim()
    {
        var restriction = Restriction.Create(RestrictionId.New(), "  Sin TACC  ", null);
        Assert.Equal("Sin TACC", restriction.Name);
    }

    [Fact]
    public void Create_NameWithMultipleSpaces_CollapsesSpaces()
    {
        var restriction = Restriction.Create(RestrictionId.New(), "Sin   TACC", null);
        Assert.Equal("Sin TACC", restriction.Name);
    }

    [Fact]
    public void Create_NullName_Throws()
    {
        Assert.Throws<RestrictionException>(() =>
            Restriction.Create(RestrictionId.New(), null!, null));
    }

    [Fact]
    public void Create_EmptyName_Throws()
    {
        Assert.Throws<RestrictionException>(() =>
            Restriction.Create(RestrictionId.New(), "", null));
    }

    [Fact]
    public void Create_WhitespaceName_Throws()
    {
        Assert.Throws<RestrictionException>(() =>
            Restriction.Create(RestrictionId.New(), "   ", null));
    }

    [Fact]
    public void Create_NameTooShort_Throws()
    {
        Assert.Throws<RestrictionException>(() =>
            Restriction.Create(RestrictionId.New(), "A", null));
    }

    [Fact]
    public void Create_NameTooLong_Throws()
    {
        string longName = new('A', Restriction.NameMaxLength + 1);
        Assert.Throws<RestrictionException>(() =>
            Restriction.Create(RestrictionId.New(), longName, null));
    }

    [Fact]
    public void Create_NameExactMinLength_Succeeds()
    {
        var restriction = Restriction.Create(RestrictionId.New(), "AB", null);
        Assert.Equal("AB", restriction.Name);
    }

    [Fact]
    public void Create_NameExactMaxLength_Succeeds()
    {
        string name = new('A', Restriction.NameMaxLength);
        var restriction = Restriction.Create(RestrictionId.New(), name, null);
        Assert.Equal(name, restriction.Name);
    }

    [Fact]
    public void Create_NullDescription_StoresNull()
    {
        var restriction = Restriction.Create(RestrictionId.New(), "Vegano", null);
        Assert.Null(restriction.Description);
    }

    [Fact]
    public void Create_EmptyDescription_NormalizesToNull()
    {
        var restriction = Restriction.Create(RestrictionId.New(), "Vegano", "");
        Assert.Null(restriction.Description);
    }

    [Fact]
    public void Create_WhitespaceDescription_NormalizesToNull()
    {
        var restriction = Restriction.Create(RestrictionId.New(), "Vegano", "   ");
        Assert.Null(restriction.Description);
    }

    [Fact]
    public void Create_DescriptionWithSpaces_AppliesTrim()
    {
        var restriction = Restriction.Create(RestrictionId.New(), "Vegano", "  Descripción  ");
        Assert.Equal("Descripción", restriction.Description);
    }

    [Fact]
    public void Create_DescriptionTooLong_Throws()
    {
        string longDesc = new('A', Restriction.DescriptionMaxLength + 1);
        Assert.Throws<RestrictionException>(() =>
            Restriction.Create(RestrictionId.New(), "Vegano", longDesc));
    }

    [Fact]
    public void Create_DescriptionExactMaxLength_Succeeds()
    {
        string desc = new('A', Restriction.DescriptionMaxLength);
        var restriction = Restriction.Create(RestrictionId.New(), "Vegano", desc);
        Assert.Equal(desc, restriction.Description);
    }

    [Fact]
    public void NoPublicSetters()
    {
        var properties = typeof(Restriction).GetProperties();
        foreach (var prop in properties)
        {
            var setter = prop.GetSetMethod(nonPublic: false);
            Assert.Null(setter);
        }
    }
}
