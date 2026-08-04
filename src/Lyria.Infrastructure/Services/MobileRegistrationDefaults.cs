using Lyria.Application.Abstractions.Services;
using Lyria.Application.Features.MobileRegistrations;
using Microsoft.Extensions.Options;

namespace Lyria.Infrastructure.Services;

internal sealed class MobileRegistrationDefaults(IOptions<MobileRegistrationOptions> options)
    : IMobileRegistrationDefaults
{
    public string DefaultRoleId => options.Value.DefaultRoleId;

    public string DefaultRestrictionImportanceLevel =>
        options.Value.DefaultRestrictionImportanceLevel;
}
