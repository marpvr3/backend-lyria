using Lyria.Application.Abstractions.Security;
using Microsoft.AspNetCore.Identity;

namespace Lyria.Infrastructure.Security;

internal sealed class PasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<object> _hasher = new();

    public string Hash(string password)
    {
        return _hasher.HashPassword(null!, password);
    }
}
