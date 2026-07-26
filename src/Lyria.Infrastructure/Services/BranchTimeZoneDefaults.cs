using Lyria.Application.Abstractions.Services;
using Lyria.Application.Features.BranchSpecialSchedules;
using Microsoft.Extensions.Options;

namespace Lyria.Infrastructure.Services;

internal sealed class BranchTimeZoneDefaults(IOptions<BranchTimeZoneOptions> options)
    : IBranchTimeZoneDefaults
{
    public string DefaultTimeZoneId => options.Value.DefaultTimeZoneId;
}
