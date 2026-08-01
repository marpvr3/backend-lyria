using Lyria.Application.Abstractions.Messaging;
using Lyria.Domain.Roles;

namespace Lyria.Application.Features.Roles.Create;

public sealed record CreateRoleCommand(
    string Code,
    string Name,
    string? Description) : ICommand<RoleId>;
