using Lyria.Application.Abstractions.Services;

namespace Lyria.Application.UnitTests.Fakes;

internal sealed class FakeBranchTimeZoneDefaults : IBranchTimeZoneDefaults
{
    public string DefaultTimeZoneId { get; set; } = "America/Argentina/Buenos_Aires";
}
