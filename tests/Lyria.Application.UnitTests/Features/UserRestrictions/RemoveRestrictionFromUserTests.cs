using Lyria.Application.Common.Errors;
using Lyria.Application.Common.Results;
using Lyria.Application.Features.UserRestrictions.Remove;
using Lyria.Application.UnitTests.Fakes;
using Lyria.Domain.Restrictions;
using Lyria.Domain.Users;
using Lyria.Domain.Users.UserRestrictions;
using Xunit;

namespace Lyria.Application.UnitTests.Features.UserRestrictions;

public sealed class RemoveRestrictionFromUserTests
{
    private static readonly DateTime CreatedAtUtc =
        new(2026, 8, 2, 5, 0, 0, DateTimeKind.Utc);

    private readonly FakeUserRestrictionRepository _userRestrictionRepository = new();
    private readonly FakeUserRepository _userRepository = new();
    private readonly FakeRestrictionRepository _restrictionRepository = new();
    private readonly RemoveRestrictionFromUserCommandHandler _handler;

    public RemoveRestrictionFromUserTests()
    {
        _handler = new RemoveRestrictionFromUserCommandHandler(_userRestrictionRepository);
    }

    private (UserId UserId, RestrictionId RestrictionId) SeedAssociation()
    {
        var userId = UserId.New();
        var restrictionId = RestrictionId.New();

        _userRepository.Seed(User.Create(
            userId, "Juan", "Pérez", "juan@example.com", "hash123", null, null, null));
        _restrictionRepository.Seed(Restriction.Create(restrictionId, "Sin TACC", null));

        _userRestrictionRepository.Seed(UserRestriction.Create(
            userId, restrictionId, UserRestrictionImportanceLevels.High, CreatedAtUtc));

        return (userId, restrictionId);
    }

    [Fact]
    public async Task Handle_WhenAssociationExists_RemovesAssociation()
    {
        (UserId userId, RestrictionId restrictionId) = SeedAssociation();

        var command = new RemoveRestrictionFromUserCommand(userId.Value, restrictionId.Value);

        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(_userRestrictionRepository.Items);
    }

    [Fact]
    public async Task Handle_WhenAssociationExists_PersistsChanges()
    {
        (UserId userId, RestrictionId restrictionId) = SeedAssociation();

        var command = new RemoveRestrictionFromUserCommand(userId.Value, restrictionId.Value);

        await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(1, _userRestrictionRepository.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_WhenAssociationExists_DoesNotRemoveUser()
    {
        (UserId userId, RestrictionId restrictionId) = SeedAssociation();

        var command = new RemoveRestrictionFromUserCommand(userId.Value, restrictionId.Value);

        await _handler.Handle(command, CancellationToken.None);

        User? user = await _userRepository.GetByIdAsync(userId, CancellationToken.None);
        Assert.NotNull(user);
    }

    [Fact]
    public async Task Handle_WhenAssociationExists_DoesNotRemoveRestriction()
    {
        (UserId userId, RestrictionId restrictionId) = SeedAssociation();

        var command = new RemoveRestrictionFromUserCommand(userId.Value, restrictionId.Value);

        await _handler.Handle(command, CancellationToken.None);

        Restriction? restriction = await _restrictionRepository.GetByIdAsync(
            restrictionId, CancellationToken.None);

        Assert.NotNull(restriction);
    }

    [Fact]
    public async Task Handle_WhenAssociationNotFound_ReturnsNotFound()
    {
        var command = new RemoveRestrictionFromUserCommand(Guid.NewGuid(), Guid.NewGuid());

        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("UserRestrictions.NotFound", result.Error.Code);
        Assert.Equal(0, _userRestrictionRepository.SaveChangesCallCount);
    }
}
