using Lyria.Application.Abstractions.Messaging;

namespace Lyria.Application.Features.Users.ChangeEmail;

public sealed record ChangeUserEmailCommand(
    Guid UserId,
    string Email) : ICommand;
