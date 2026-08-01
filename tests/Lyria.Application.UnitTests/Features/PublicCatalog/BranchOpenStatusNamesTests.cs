using Lyria.Application.Features.PublicCatalog;
using Lyria.Domain.Establishments.Branches;
using Xunit;

namespace Lyria.Application.UnitTests.Features.PublicCatalog;

public sealed class BranchOpenStatusNamesTests
{
    [Theory]
    [InlineData(BranchOpenStatus.Open, "Abierto")]
    [InlineData(BranchOpenStatus.OpensLaterToday, "Abre más tarde")]
    [InlineData(BranchOpenStatus.Closed, "Cerrado")]
    [InlineData(BranchOpenStatus.NoSchedule, "Horario no disponible")]
    public void GetName_ReturnsCorrectSpanishName(BranchOpenStatus status, string expectedName)
    {
        string name = BranchOpenStatusNames.GetName(status);
        Assert.Equal(expectedName, name);
    }

    [Fact]
    public void GetName_UnknownValue_ReturnsDesconocido()
    {
        string name = BranchOpenStatusNames.GetName((BranchOpenStatus)99);
        Assert.Equal("Desconocido", name);
    }
}
