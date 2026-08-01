using Lyria.Application.Abstractions.Security;

namespace Lyria.Application.UnitTests.Fakes;

internal sealed class FakePasswordHasher : IPasswordHasher
{
    public string Hash(string password) => "hashed_" + password;
}
