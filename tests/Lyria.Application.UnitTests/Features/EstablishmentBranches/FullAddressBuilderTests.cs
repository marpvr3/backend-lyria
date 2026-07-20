using Lyria.Application.Features.EstablishmentBranches;
using Xunit;

namespace Lyria.Application.UnitTests.Features.EstablishmentBranches;

public sealed class FullAddressBuilderTests
{
    [Fact]
    public void Build_AllParts_ReturnsFullAddress()
    {
        string result = FullAddressBuilder.Build(
            "Costa Rica", "5865", "Piso 2", "Palermo",
            "Buenos Aires", "Buenos Aires", "C1414", "Argentina");

        Assert.Equal(
            "Costa Rica 5865, Piso 2, Palermo, Buenos Aires, Buenos Aires, C1414, Argentina",
            result);
    }

    [Fact]
    public void Build_NullsOmitted_NoDoubleCommas()
    {
        string result = FullAddressBuilder.Build(
            "Costa Rica", "5865", null, null,
            "Buenos Aires", null, null, "Argentina");

        Assert.Equal("Costa Rica 5865, Buenos Aires, Argentina", result);
        Assert.DoesNotContain(",,", result);
    }

    [Fact]
    public void Build_OnlyStreet_ReturnsStreet()
    {
        string result = FullAddressBuilder.Build(
            "Costa Rica", null, null, null,
            null, null, null, null);

        Assert.Equal("Costa Rica", result);
    }
}
