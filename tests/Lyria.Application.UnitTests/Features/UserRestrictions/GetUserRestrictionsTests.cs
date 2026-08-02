using Lyria.Application.Common.Errors;
using Lyria.Application.Common.Results;
using Lyria.Application.Features.UserRestrictions;
using Lyria.Application.Features.UserRestrictions.GetByUserId;
using Lyria.Application.Features.Users;
using Lyria.Application.UnitTests.Fakes;
using Lyria.Domain.Users;
using Lyria.Domain.Users.UserRestrictions;
using Xunit;

namespace Lyria.Application.UnitTests.Features.UserRestrictions;

public sealed class GetUserRestrictionsTests
{
    private static readonly DateTime CreatedAtUtc =
        new(2026, 8, 2, 5, 0, 0, DateTimeKind.Utc);

    private readonly FakeUserRestrictionReadService _userRestrictionReadService = new();
    private readonly FakeUserReadService _userReadService = new();
    private readonly GetUserRestrictionsQueryHandler _handler;

    public GetUserRestrictionsTests()
    {
        _handler = new GetUserRestrictionsQueryHandler(
            _userRestrictionReadService, _userReadService);
    }

    private Guid SeedUser()
    {
        var userId = Guid.NewGuid();

        _userReadService.Seed(new UserResponse(
            userId, "Juan", "Pérez", "juan@example.com", null, null, null,
            "Unverified", false, null, CreatedAtUtc, null));

        return userId;
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsNotFound()
    {
        var query = new GetUserRestrictionsQuery(Guid.NewGuid());

        Result<IReadOnlyList<UserRestrictionResponse>> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("UserRestrictions.UserNotFound", result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenUserHasNoRestrictions_ReturnsEmptyList()
    {
        Guid userId = SeedUser();

        var query = new GetUserRestrictionsQuery(userId);

        Result<IReadOnlyList<UserRestrictionResponse>> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task Handle_WhenUserHasRestrictions_ReturnsAllOfThem()
    {
        Guid userId = SeedUser();

        _userRestrictionReadService.Seed(new UserRestrictionResponse(
            userId, Guid.NewGuid(), "Sin TACC",
            UserRestrictionImportanceLevels.High, CreatedAtUtc));

        _userRestrictionReadService.Seed(new UserRestrictionResponse(
            userId, Guid.NewGuid(), "Sin lactosa",
            UserRestrictionImportanceLevels.Low, CreatedAtUtc));

        var query = new GetUserRestrictionsQuery(userId);

        Result<IReadOnlyList<UserRestrictionResponse>> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
    }

    [Fact]
    public async Task Handle_ReturnsOnlyRestrictionsOfRequestedUser()
    {
        Guid userId = SeedUser();

        _userRestrictionReadService.Seed(new UserRestrictionResponse(
            userId, Guid.NewGuid(), "Sin TACC",
            UserRestrictionImportanceLevels.High, CreatedAtUtc));

        _userRestrictionReadService.Seed(new UserRestrictionResponse(
            Guid.NewGuid(), Guid.NewGuid(), "Sin lactosa",
            UserRestrictionImportanceLevels.Low, CreatedAtUtc));

        var query = new GetUserRestrictionsQuery(userId);

        Result<IReadOnlyList<UserRestrictionResponse>> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.Single(result.Value);
        Assert.Equal(userId, result.Value[0].UserId);
    }

    [Fact]
    public async Task Handle_IncludesRestrictionName()
    {
        Guid userId = SeedUser();
        var restrictionId = Guid.NewGuid();

        _userRestrictionReadService.Seed(new UserRestrictionResponse(
            userId, restrictionId, "Sin TACC",
            UserRestrictionImportanceLevels.High, CreatedAtUtc));

        var query = new GetUserRestrictionsQuery(userId);

        Result<IReadOnlyList<UserRestrictionResponse>> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.Equal("Sin TACC", result.Value[0].RestrictionName);
    }
}
