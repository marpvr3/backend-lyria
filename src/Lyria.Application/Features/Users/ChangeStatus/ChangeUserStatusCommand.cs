using Lyria.Application.Abstractions.Messaging;

namespace Lyria.Application.Features.Users.ChangeStatus;

public sealed record ChangeUserStatusCommand(
    Guid UserId,
    string Status) : ICommand;
