using Lyria.Domain.Establishments;
using Lyria.Domain.Establishments.Branches;
using Lyria.Domain.Roles;
using Lyria.Domain.Users;
using Lyria.Domain.Users.UserRoles;
using Xunit;

namespace Lyria.Domain.UnitTests.Users.UserRoles;

public sealed class UserRoleAssignTests
{
    private static readonly UserRoleId DefaultId = UserRoleId.New();
    private static readonly UserId DefaultUserId = UserId.New();
    private static readonly RoleId DefaultRoleId = RoleId.New();
    private static readonly DateTime DefaultAssignedAt = new(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Assign_WithGlobalScope_CreatesAssignment()
    {
        var userRole = UserRole.Assign(
            DefaultId, DefaultUserId, DefaultRoleId,
            ScopeType.Global, null, null, DefaultAssignedAt);

        Assert.Equal(DefaultId, userRole.Id);
        Assert.Equal(DefaultUserId, userRole.UserId);
        Assert.Equal(DefaultRoleId, userRole.RoleId);
        Assert.Equal(ScopeType.Global, userRole.ScopeType);
        Assert.Null(userRole.EstablishmentId);
        Assert.Null(userRole.BranchId);
        Assert.True(userRole.IsActive);
        Assert.Null(userRole.EndedAtUtc);
    }

    [Fact]
    public void Assign_WithEstablishmentScope_CreatesAssignment()
    {
        var establishmentId = EstablishmentId.New();

        var userRole = UserRole.Assign(
            DefaultId, DefaultUserId, DefaultRoleId,
            ScopeType.Establishment, establishmentId, null, DefaultAssignedAt);

        Assert.Equal(ScopeType.Establishment, userRole.ScopeType);
        Assert.Equal(establishmentId, userRole.EstablishmentId);
        Assert.Null(userRole.BranchId);
    }

    [Fact]
    public void Assign_WithBranchScope_CreatesAssignment()
    {
        var branchId = EstablishmentBranchId.New();

        var userRole = UserRole.Assign(
            DefaultId, DefaultUserId, DefaultRoleId,
            ScopeType.Branch, null, branchId, DefaultAssignedAt);

        Assert.Equal(ScopeType.Branch, userRole.ScopeType);
        Assert.Equal(branchId, userRole.BranchId);
    }

    [Fact]
    public void Assign_WithBranchScopeAndEstablishment_CreatesAssignment()
    {
        var establishmentId = EstablishmentId.New();
        var branchId = EstablishmentBranchId.New();

        var userRole = UserRole.Assign(
            DefaultId, DefaultUserId, DefaultRoleId,
            ScopeType.Branch, establishmentId, branchId, DefaultAssignedAt);

        Assert.Equal(ScopeType.Branch, userRole.ScopeType);
        Assert.Equal(establishmentId, userRole.EstablishmentId);
        Assert.Equal(branchId, userRole.BranchId);
    }

    [Fact]
    public void Assign_WithEmptyUserId_ThrowsUserRoleException()
    {
        Assert.Throws<UserRoleException>(() =>
            UserRole.Assign(
                DefaultId, new UserId(Guid.Empty), DefaultRoleId,
                ScopeType.Global, null, null, DefaultAssignedAt));
    }

    [Fact]
    public void Assign_WithEmptyRoleId_ThrowsUserRoleException()
    {
        Assert.Throws<UserRoleException>(() =>
            UserRole.Assign(
                DefaultId, DefaultUserId, new RoleId(Guid.Empty),
                ScopeType.Global, null, null, DefaultAssignedAt));
    }

    [Fact]
    public void Assign_GlobalWithEstablishmentId_ThrowsUserRoleException()
    {
        Assert.Throws<UserRoleException>(() =>
            UserRole.Assign(
                DefaultId, DefaultUserId, DefaultRoleId,
                ScopeType.Global, EstablishmentId.New(), null, DefaultAssignedAt));
    }

    [Fact]
    public void Assign_GlobalWithBranchId_ThrowsUserRoleException()
    {
        Assert.Throws<UserRoleException>(() =>
            UserRole.Assign(
                DefaultId, DefaultUserId, DefaultRoleId,
                ScopeType.Global, null, EstablishmentBranchId.New(), DefaultAssignedAt));
    }

    [Fact]
    public void Assign_EstablishmentWithoutEstablishmentId_ThrowsUserRoleException()
    {
        Assert.Throws<UserRoleException>(() =>
            UserRole.Assign(
                DefaultId, DefaultUserId, DefaultRoleId,
                ScopeType.Establishment, null, null, DefaultAssignedAt));
    }

    [Fact]
    public void Assign_EstablishmentWithBranchId_ThrowsUserRoleException()
    {
        Assert.Throws<UserRoleException>(() =>
            UserRole.Assign(
                DefaultId, DefaultUserId, DefaultRoleId,
                ScopeType.Establishment, EstablishmentId.New(), EstablishmentBranchId.New(), DefaultAssignedAt));
    }

    [Fact]
    public void Assign_BranchWithoutBranchId_ThrowsUserRoleException()
    {
        Assert.Throws<UserRoleException>(() =>
            UserRole.Assign(
                DefaultId, DefaultUserId, DefaultRoleId,
                ScopeType.Branch, null, null, DefaultAssignedAt));
    }

    [Fact]
    public void Assign_SetsAssignedAtUtc()
    {
        var userRole = UserRole.Assign(
            DefaultId, DefaultUserId, DefaultRoleId,
            ScopeType.Global, null, null, DefaultAssignedAt);

        Assert.Equal(DefaultAssignedAt, userRole.AssignedAtUtc);
    }
}
