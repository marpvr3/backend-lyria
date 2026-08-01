using Lyria.Application.Abstractions.Messaging;

namespace Lyria.Application.Features.Users.Update;

public sealed record UpdateUserCommand(
    Guid UserId,
    string Name,
    string LastName,
    string? Phone,
    DateOnly? BirthDate,
    string? PhotoUrl) : ICommand;
