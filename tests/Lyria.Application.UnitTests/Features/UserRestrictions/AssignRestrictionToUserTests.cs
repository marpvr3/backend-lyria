using Lyria.Application.Common.Errors;
using Lyria.Application.Common.Results;
using Lyria.Application.Features.UserRestrictions.Assign;
using Lyria.Application.UnitTests.Fakes;
using Lyria.Domain.Restrictions;
using Lyria.Domain.Users;
using Lyria.Domain.Users.UserRestrictions;
using Xunit;

namespace Lyria.Application.UnitTests.Features.UserRestrictions;

public sealed class AssignRestrictionToUserTests
{
    private static readonly DateTimeOffset FixedTime =
        new(2026, 8, 2, 5, 0, 0, TimeSpan.Zero);

    private readonly FakeUserRestrictionRepository _userRestrictionRepository = new();
    private readonly FakeUserRepository _userRepository = new();
    private readonly FakeRestrictionRepository _restrictionRepository = new();
    private readonly FixedTimeProvider _timeProvider = new(FixedTime);
    private readonly AssignRestrictionToUserCommandHandler _handler;

    public AssignRestrictionToUserTests()
    {
        _handler = new AssignRestrictionToUserCommandHandler(
            _userRestrictionRepository,
            _userRepository,
            _restrictionRepository,
            _timeProvider);
    }

    private UserId SeedUser()
    {
        var userId = UserId.New();
        _userRepository.Seed(User.Create(
            userId, "Juan", "Pérez", "juan@example.com", "hash123", null, null, null));

        return userId;
    }

    private RestrictionId SeedRestriction()
    {
        var restrictionId = RestrictionId.New();
        _restrictionRepository.Seed(Restriction.Create(restrictionId, "Sin TACC", null));

        return restrictionId;
    }

    [Fact]
    public async Task Handle_WhenAllValid_CreatesAssociation()
    {
        UserId userId = SeedUser();
        RestrictionId restrictionId = SeedRestriction();

        var command = new AssignRestrictionToUserCommand(
            userId.Value, restrictionId.Value, UserRestrictionImportanceLevels.High);

        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(_userRestrictionRepository.Items);
    }

    [Fact]
    public async Task Handle_WhenAllValid_PersistsChanges()
    {
        UserId userId = SeedUser();
        RestrictionId restrictionId = SeedRestriction();

        var command = new AssignRestrictionToUserCommand(
            userId.Value, restrictionId.Value, UserRestrictionImportanceLevels.Medium);

        await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(1, _userRestrictionRepository.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_WhenAllValid_StoresProvidedData()
    {
        UserId userId = SeedUser();
        RestrictionId restrictionId = SeedRestriction();

        var command = new AssignRestrictionToUserCommand(
            userId.Value, restrictionId.Value, UserRestrictionImportanceLevels.Low);

        await _handler.Handle(command, CancellationToken.None);

        UserRestriction created = _userRestrictionRepository.Items[0];

        Assert.Equal(userId, created.UserId);
        Assert.Equal(restrictionId, created.RestrictionId);
        Assert.Equal(UserRestrictionImportanceLevels.Low, created.ImportanceLevel);
    }

    [Fact]
    public async Task Handle_WhenAllValid_UsesTimeProviderForCreatedAtUtc()
    {
        UserId userId = SeedUser();
        RestrictionId restrictionId = SeedRestriction();

        var command = new AssignRestrictionToUserCommand(
            userId.Value, restrictionId.Value, UserRestrictionImportanceLevels.High);

        await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(
            FixedTime.UtcDateTime,
            _userRestrictionRepository.Items[0].CreatedAtUtc);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsNotFound()
    {
        RestrictionId restrictionId = SeedRestriction();

        var command = new AssignRestrictionToUserCommand(
            Guid.NewGuid(), restrictionId.Value, UserRestrictionImportanceLevels.High);

        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("UserRestrictions.UserNotFound", result.Error.Code);
        Assert.Empty(_userRestrictionRepository.Items);
    }

    [Fact]
    public async Task Handle_WhenRestrictionNotFound_ReturnsNotFound()
    {
        UserId userId = SeedUser();

        var command = new AssignRestrictionToUserCommand(
            userId.Value, Guid.NewGuid(), UserRestrictionImportanceLevels.High);

        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("UserRestrictions.RestrictionNotFound", result.Error.Code);
        Assert.Empty(_userRestrictionRepository.Items);
    }

    [Fact]
    public async Task Handle_WhenAssociationAlreadyExists_ReturnsConflict()
    {
        UserId userId = SeedUser();
        RestrictionId restrictionId = SeedRestriction();

        _userRestrictionRepository.Seed(UserRestriction.Create(
            userId, restrictionId, UserRestrictionImportanceLevels.Low, FixedTime.UtcDateTime));

        var command = new AssignRestrictionToUserCommand(
            userId.Value, restrictionId.Value, UserRestrictionImportanceLevels.High);

        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("UserRestrictions.AlreadyExists", result.Error.Code);
        Assert.Single(_userRestrictionRepository.Items);
    }

    [Fact]
    public async Task Handle_WhenFailure_DoesNotPersistChanges()
    {
        var command = new AssignRestrictionToUserCommand(
            Guid.NewGuid(), Guid.NewGuid(), UserRestrictionImportanceLevels.High);

        await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(0, _userRestrictionRepository.SaveChangesCallCount);
    }
}
