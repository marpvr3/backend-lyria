using Lyria.Application.Abstractions.Messaging;

namespace Lyria.Application.Features.Roles.Update;

public sealed record UpdateRoleCommand(
    Guid Id,
    string Name,
    string? Description) : ICommand;
