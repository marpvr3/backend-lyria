using Lyria.Application.Abstractions.Messaging;

namespace Lyria.Application.Features.Users.ChangePassword;

public sealed record ChangeUserPasswordCommand(
    Guid UserId,
    string Password) : ICommand;
