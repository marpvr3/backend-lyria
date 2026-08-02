using Lyria.Application.Common.Errors;
using Lyria.Application.Common.Results;
using Lyria.Application.Features.UserRestrictions.Update;
using Lyria.Application.UnitTests.Fakes;
using Lyria.Domain.Restrictions;
using Lyria.Domain.Users;
using Lyria.Domain.Users.UserRestrictions;
using Xunit;

namespace Lyria.Application.UnitTests.Features.UserRestrictions;

public sealed class UpdateUserRestrictionTests
{
    private static readonly DateTime CreatedAtUtc =
        new(2026, 8, 2, 5, 0, 0, DateTimeKind.Utc);

    private readonly FakeUserRestrictionRepository _repository = new();
    private readonly UpdateUserRestrictionCommandHandler _handler;

    public UpdateUserRestrictionTests()
    {
        _handler = new UpdateUserRestrictionCommandHandler(_repository);
    }

    private (UserId UserId, RestrictionId RestrictionId) SeedAssociation(
        string importanceLevel = UserRestrictionImportanceLevels.High)
    {
        var userId = UserId.New();
        var restrictionId = RestrictionId.New();

        _repository.Seed(UserRestriction.Create(
            userId, restrictionId, importanceLevel, CreatedAtUtc));

        return (userId, restrictionId);
    }

    [Fact]
    public async Task Handle_WhenAssociationExists_UpdatesImportanceLevel()
    {
        (UserId userId, RestrictionId restrictionId) = SeedAssociation();

        var command = new UpdateUserRestrictionCommand(
            userId.Value, restrictionId.Value, UserRestrictionImportanceLevels.Medium);

        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(
            UserRestrictionImportanceLevels.Medium,
            _repository.Items[0].ImportanceLevel);
    }

    [Fact]
    public async Task Handle_WhenAssociationExists_PersistsChanges()
    {
        (UserId userId, RestrictionId restrictionId) = SeedAssociation();

        var command = new UpdateUserRestrictionCommand(
            userId.Value, restrictionId.Value, UserRestrictionImportanceLevels.Low);

        await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(1, _repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_WhenAssociationExists_PreservesCreatedAtUtc()
    {
        (UserId userId, RestrictionId restrictionId) = SeedAssociation();

        var command = new UpdateUserRestrictionCommand(
            userId.Value, restrictionId.Value, UserRestrictionImportanceLevels.Low);

        await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(CreatedAtUtc, _repository.Items[0].CreatedAtUtc);
    }

    [Fact]
    public async Task Handle_WhenAssociationExists_DoesNotCreateNewAssociation()
    {
        (UserId userId, RestrictionId restrictionId) = SeedAssociation();

        var command = new UpdateUserRestrictionCommand(
            userId.Value, restrictionId.Value, UserRestrictionImportanceLevels.Low);

        await _handler.Handle(command, CancellationToken.None);

        Assert.Single(_repository.Items);
    }

    [Fact]
    public async Task Handle_WhenAssociationNotFound_ReturnsNotFound()
    {
        var command = new UpdateUserRestrictionCommand(
            Guid.NewGuid(), Guid.NewGuid(), UserRestrictionImportanceLevels.Low);

        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("UserRestrictions.NotFound", result.Error.Code);
        Assert.Equal(0, _repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_WhenImportanceLevelIsInvalid_Throws()
    {
        (UserId userId, RestrictionId restrictionId) = SeedAssociation();

        var command = new UpdateUserRestrictionCommand(
            userId.Value, restrictionId.Value, "Critical");

        await Assert.ThrowsAsync<UserRestrictionException>(async () =>
            await _handler.Handle(command, CancellationToken.None));
    }
}
