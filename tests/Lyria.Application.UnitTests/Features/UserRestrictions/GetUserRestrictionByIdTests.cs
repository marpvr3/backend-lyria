using Lyria.Application.Common.Errors;
using Lyria.Application.Common.Results;
using Lyria.Application.Features.UserRestrictions;
using Lyria.Application.Features.UserRestrictions.GetById;
using Lyria.Application.UnitTests.Fakes;
using Lyria.Domain.Users.UserRestrictions;
using Xunit;

namespace Lyria.Application.UnitTests.Features.UserRestrictions;

public sealed class GetUserRestrictionByIdTests
{
    private static readonly DateTime CreatedAtUtc =
        new(2026, 8, 2, 5, 0, 0, DateTimeKind.Utc);

    private readonly FakeUserRestrictionReadService _readService = new();
    private readonly GetUserRestrictionByIdQueryHandler _handler;

    public GetUserRestrictionByIdTests()
    {
        _handler = new GetUserRestrictionByIdQueryHandler(_readService);
    }

    [Fact]
    public async Task Handle_WhenAssociationExists_ReturnsResponse()
    {
        var userId = Guid.NewGuid();
        var restrictionId = Guid.NewGuid();

        _readService.Seed(new UserRestrictionResponse(
            userId, restrictionId, "Sin TACC",
            UserRestrictionImportanceLevels.High, CreatedAtUtc));

        var query = new GetUserRestrictionByIdQuery(userId, restrictionId);

        Result<UserRestrictionResponse> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(userId, result.Value.UserId);
        Assert.Equal(restrictionId, result.Value.RestrictionId);
        Assert.Equal("Sin TACC", result.Value.RestrictionName);
        Assert.Equal(UserRestrictionImportanceLevels.High, result.Value.ImportanceLevel);
        Assert.Equal(CreatedAtUtc, result.Value.CreatedAtUtc);
    }

    [Fact]
    public async Task Handle_WhenAssociationNotFound_ReturnsNotFound()
    {
        var query = new GetUserRestrictionByIdQuery(Guid.NewGuid(), Guid.NewGuid());

        Result<UserRestrictionResponse> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("UserRestrictions.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenRestrictionBelongsToAnotherUser_ReturnsNotFound()
    {
        var restrictionId = Guid.NewGuid();

        _readService.Seed(new UserRestrictionResponse(
            Guid.NewGuid(), restrictionId, "Sin TACC",
            UserRestrictionImportanceLevels.High, CreatedAtUtc));

        var query = new GetUserRestrictionByIdQuery(Guid.NewGuid(), restrictionId);

        Result<UserRestrictionResponse> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("UserRestrictions.NotFound", result.Error.Code);
    }
}
