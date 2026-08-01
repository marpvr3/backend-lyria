using Lyria.Domain.Abstractions;
using Lyria.Domain.Establishments;
using Lyria.Domain.Establishments.Branches;
using Lyria.Domain.Roles;

namespace Lyria.Domain.Users.UserRoles;

public sealed class UserRole : Entity<UserRoleId>
{
    public UserId UserId { get; private set; }
    public RoleId RoleId { get; private set; }
    public ScopeType ScopeType { get; private set; }
    public EstablishmentId? EstablishmentId { get; private set; }
    public EstablishmentBranchId? BranchId { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime AssignedAtUtc { get; private set; }
    public DateTime? EndedAtUtc { get; private set; }

    private UserRole()
    {
    }

    private UserRole(
        UserRoleId id,
        UserId userId,
        RoleId roleId,
        ScopeType scopeType,
        EstablishmentId? establishmentId,
        EstablishmentBranchId? branchId,
        DateTime assignedAtUtc)
        : base(id)
    {
        UserId = userId;
        RoleId = roleId;
        ScopeType = scopeType;
        EstablishmentId = establishmentId;
        BranchId = branchId;
        IsActive = true;
        AssignedAtUtc = assignedAtUtc;
        EndedAtUtc = null;
    }

    public static UserRole Assign(
        UserRoleId id,
        UserId userId,
        RoleId roleId,
        ScopeType scopeType,
        EstablishmentId? establishmentId,
        EstablishmentBranchId? branchId,
        DateTime assignedAtUtc)
    {
        ValidateUserId(userId);
        ValidateRoleId(roleId);
        ValidateScope(scopeType, establishmentId, branchId);

        return new UserRole(id, userId, roleId, scopeType, establishmentId, branchId, assignedAtUtc);
    }

    public void Activate()
    {
        if (IsActive)
        {
            throw new UserRoleException("La asignación de rol ya se encuentra activa.");
        }

        IsActive = true;
    }

    public void Deactivate()
    {
        if (!IsActive)
        {
            throw new UserRoleException("La asignación de rol ya se encuentra inactiva.");
        }

        IsActive = false;
    }

    public void Finalize(DateTime endedAtUtc)
    {
        if (EndedAtUtc is not null)
        {
            throw new UserRoleException("La asignación de rol ya ha sido finalizada.");
        }

        IsActive = false;
        EndedAtUtc = endedAtUtc;
    }

    private static void ValidateUserId(UserId userId)
    {
        if (userId.Value == Guid.Empty)
        {
            throw new UserRoleException("El usuario es obligatorio.");
        }
    }

    private static void ValidateRoleId(RoleId roleId)
    {
        if (roleId.Value == Guid.Empty)
        {
            throw new UserRoleException("El rol es obligatorio.");
        }
    }

    private static void ValidateScope(
        ScopeType scopeType,
        EstablishmentId? establishmentId,
        EstablishmentBranchId? branchId)
    {
        switch (scopeType)
        {
            case ScopeType.Global:
                if (establishmentId is not null)
                {
                    throw new UserRoleException(
                        "El alcance global no permite especificar un establecimiento.");
                }

                if (branchId is not null)
                {
                    throw new UserRoleException(
                        "El alcance global no permite especificar una sede.");
                }

                break;

            case ScopeType.Establishment:
                if (establishmentId is null)
                {
                    throw new UserRoleException(
                        "El alcance de establecimiento requiere un establecimiento.");
                }

                if (branchId is not null)
                {
                    throw new UserRoleException(
                        "El alcance de establecimiento no permite especificar una sede.");
                }

                break;

            case ScopeType.Branch:
                if (branchId is null)
                {
                    throw new UserRoleException(
                        "El alcance de sede requiere una sede.");
                }

                break;

            default:
                throw new UserRoleException(
                    $"El tipo de alcance '{scopeType}' no es válido.");
        }
    }
}
