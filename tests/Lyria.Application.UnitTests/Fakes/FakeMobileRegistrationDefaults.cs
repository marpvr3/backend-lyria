using Lyria.Application.Abstractions.Services;
using Lyria.Domain.Users.UserRestrictions;

namespace Lyria.Application.UnitTests.Fakes;

internal sealed class FakeMobileRegistrationDefaults : IMobileRegistrationDefaults
{
    public string DefaultRoleId { get; set; } = Guid.NewGuid().ToString();

    public string DefaultRestrictionImportanceLevel { get; set; } =
        UserRestrictionImportanceLevels.High;
}
