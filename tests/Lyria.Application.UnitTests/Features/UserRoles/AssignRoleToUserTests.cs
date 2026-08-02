using Lyria.Application.Common.Errors;
using Lyria.Application.Common.Results;
using Lyria.Application.Features.UserRoles.Assign;
using Lyria.Application.UnitTests.Fakes;
using Lyria.Domain.Roles;
using Lyria.Domain.Users;
using Lyria.Domain.Users.UserRoles;
using Xunit;

namespace Lyria.Application.UnitTests.Features.UserRoles;

public sealed class AssignRoleToUserTests
{
    private readonly FakeUserRoleRepository _userRoleRepository = new();
    private readonly FakeUserRepository _userRepository = new();
    private readonly FakeRoleRepository _roleRepository = new();
    private readonly FakeEstablishmentReadService _establishmentReadService = new();
    private readonly FakeEstablishmentBranchReadService _branchReadService = new();
    private readonly TimeProvider _timeProvider = TimeProvider.System;
    private readonly AssignRoleToUserCommandHandler _handler;

    public AssignRoleToUserTests()
    {
        _handler = new AssignRoleToUserCommandHandler(
            _userRoleRepository,
            _userRepository,
            _roleRepository,
            _establishmentReadService,
            _branchReadService,
            _timeProvider);
    }

    [Fact]
    public async Task Handle_WhenAllValid_AssignsRole()
    {
        var userId = UserId.New();
        _userRepository.Seed(User.Create(
            userId, "Juan", "Pérez", "juan@example.com", "hash123", null, null, null));

        var roleId = RoleId.New();
        _roleRepository.Seed(Role.Create(roleId, "ADMIN", "Administrador", null));

        var command = new AssignRoleToUserCommand(
            userId.Value, roleId.Value, "Global", null, null);

        Result<UserRoleId> result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(_userRoleRepository.Added);
        Assert.Equal(1, _userRoleRepository.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsError()
    {
        var roleId = RoleId.New();
        _roleRepository.Seed(Role.Create(roleId, "ADMIN", "Administrador", null));

        var command = new AssignRoleToUserCommand(
            Guid.NewGuid(), roleId.Value, "Global", null, null);

        Result<UserRoleId> result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("UserRoles.UserNotFound", result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenRoleNotFound_ReturnsError()
    {
        var userId = UserId.New();
        _userRepository.Seed(User.Create(
            userId, "Juan", "Pérez", "juan@example.com", "hash123", null, null, null));

        var command = new AssignRoleToUserCommand(
            userId.Value, Guid.NewGuid(), "Global", null, null);

        Result<UserRoleId> result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("UserRoles.RoleNotFound", result.Error.Code);
    }

    [Fact]
    public async Task Handle_WithInvalidScopeType_ReturnsError()
    {
        var userId = UserId.New();
        _userRepository.Seed(User.Create(
            userId, "Juan", "Pérez", "juan@example.com", "hash123", null, null, null));

        var roleId = RoleId.New();
        _roleRepository.Seed(Role.Create(roleId, "ADMIN", "Administrador", null));

        var command = new AssignRoleToUserCommand(
            userId.Value, roleId.Value, "InvalidScope", null, null);

        Result<UserRoleId> result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("UserRoles.InvalidScope", result.Error.Code);
    }

    [Fact]
    public async Task Handle_BranchScope_WhenBranchDoesNotBelongToEstablishment_ReturnsError()
    {
        var userId = UserId.New();
        _userRepository.Seed(User.Create(
            userId, "Juan", "Pérez", "juan@example.com", "hash123", null, null, null));

        var roleId = RoleId.New();
        _roleRepository.Seed(Role.Create(roleId, "ADMIN", "Administrador", null));

        var establishmentId = Guid.NewGuid();
        _establishmentReadService.Seed(new Application.Features.Establishments.EstablishmentResponse(
            establishmentId, Guid.NewGuid(), "Cat", "Est", "est", null, null, null, null, null, null,
            false, null, true, DateTime.UtcNow, null));

        var otherEstablishmentId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        _branchReadService.Seed(new Application.Features.EstablishmentBranches.EstablishmentBranchResponse(
            branchId, otherEstablishmentId, "Sede", "Calle 1", null, null, null, null, null, null, null,
            "Calle 1", null, null, null, null, null, 0m, 0, true, "America/Bogota",
            DateTime.UtcNow, null));

        var command = new AssignRoleToUserCommand(
            userId.Value, roleId.Value, "Branch", establishmentId, branchId);

        Result<UserRoleId> result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("UserRoles.BranchDoesNotBelongToEstablishment", result.Error.Code);
        Assert.Empty(_userRoleRepository.Added);
    }

    [Fact]
    public async Task Handle_BranchScope_WhenBranchBelongsToEstablishment_Succeeds()
    {
        var userId = UserId.New();
        _userRepository.Seed(User.Create(
            userId, "Juan", "Pérez", "juan@example.com", "hash123", null, null, null));

        var roleId = RoleId.New();
        _roleRepository.Seed(Role.Create(roleId, "ADMIN", "Administrador", null));

        var establishmentId = Guid.NewGuid();
        _establishmentReadService.Seed(new Application.Features.Establishments.EstablishmentResponse(
            establishmentId, Guid.NewGuid(), "Cat", "Est", "est", null, null, null, null, null, null,
            false, null, true, DateTime.UtcNow, null));

        var branchId = Guid.NewGuid();
        _branchReadService.Seed(new Application.Features.EstablishmentBranches.EstablishmentBranchResponse(
            branchId, establishmentId, "Sede", "Calle 1", null, null, null, null, null, null, null,
            "Calle 1", null, null, null, null, null, 0m, 0, true, "America/Bogota",
            DateTime.UtcNow, null));

        var command = new AssignRoleToUserCommand(
            userId.Value, roleId.Value, "Branch", establishmentId, branchId);

        Result<UserRoleId> result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(_userRoleRepository.Added);
    }
}
