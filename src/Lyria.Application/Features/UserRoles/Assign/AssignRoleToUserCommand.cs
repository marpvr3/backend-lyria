using Lyria.Application.Abstractions.Messaging;
using Lyria.Domain.Users.UserRoles;

namespace Lyria.Application.Features.UserRoles.Assign;

public sealed record AssignRoleToUserCommand(
    Guid UserId,
    Guid RoleId,
    string ScopeType,
    Guid? EstablishmentId,
    Guid? BranchId) : ICommand<UserRoleId>;
