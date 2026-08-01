using Lyria.Application.Abstractions.Messaging;
using Lyria.Domain.Users;

namespace Lyria.Application.Features.Users.Create;

public sealed record CreateUserCommand(
    string Name,
    string LastName,
    string Email,
    string Password,
    string? Phone,
    DateOnly? BirthDate,
    string? PhotoUrl) : ICommand<UserId>;
