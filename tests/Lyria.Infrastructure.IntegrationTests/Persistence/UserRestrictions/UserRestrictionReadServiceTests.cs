using Lyria.Application.Features.UserRestrictions;
using Lyria.Domain.Restrictions;
using Lyria.Domain.Users;
using Lyria.Domain.Users.UserRestrictions;
using Lyria.Infrastructure.Persistence.ReadServices;
using Xunit;

namespace Lyria.Infrastructure.IntegrationTests.Persistence.UserRestrictions;

public sealed class UserRestrictionReadServiceTests : IDisposable
{
    private static readonly DateTime CreatedAtUtc =
        new(2026, 8, 2, 5, 0, 0, DateTimeKind.Utc);

    private readonly SqliteFixture _fixture = new();

    private async Task<UserId> SeedUserAsync()
    {
        var user = User.Create(
            UserId.New(), "Juan", "Garcia", $"{Guid.NewGuid():N}@example.com",
            "hashed_pw", null, null, null);

        await using var context = _fixture.CreateContext();
        context.Set<User>().Add(user);
        await context.SaveChangesAsync(CancellationToken.None);

        return user.Id;
    }

    private async Task<RestrictionId> SeedRestrictionAsync(string name)
    {
        var restriction = Restriction.Create(RestrictionId.New(), name, null);

        await using var context = _fixture.CreateContext();
        context.Set<Restriction>().Add(restriction);
        await context.SaveChangesAsync(CancellationToken.None);

        return restriction.Id;
    }

    private async Task SeedAssociationAsync(
        UserId userId,
        RestrictionId restrictionId,
        string importanceLevel)
    {
        await using var context = _fixture.CreateContext();
        context.Set<UserRestriction>().Add(UserRestriction.Create(
            userId, restrictionId, importanceLevel, CreatedAtUtc));
        await context.SaveChangesAsync(CancellationToken.None);
    }

    [Fact]
    public async Task GetByUserIdAsync_Should_Return_Associations_WithRestrictionName()
    {
        UserId userId = await SeedUserAsync();
        RestrictionId restrictionId = await SeedRestrictionAsync("Sin TACC");
        await SeedAssociationAsync(userId, restrictionId, UserRestrictionImportanceLevels.High);

        await using var context = _fixture.CreateContext();
        var readService = new UserRestrictionReadService(context);

        IReadOnlyList<UserRestrictionResponse> result =
            await readService.GetByUserIdAsync(userId, CancellationToken.None);

        UserRestrictionResponse item = Assert.Single(result);
        Assert.Equal(userId.Value, item.UserId);
        Assert.Equal(restrictionId.Value, item.RestrictionId);
        Assert.Equal("Sin TACC", item.RestrictionName);
        Assert.Equal(UserRestrictionImportanceLevels.High, item.ImportanceLevel);
        Assert.Equal(CreatedAtUtc, item.CreatedAtUtc);
    }

    [Fact]
    public async Task GetByUserIdAsync_Should_Return_Empty_WhenUserHasNoAssociations()
    {
        UserId userId = await SeedUserAsync();

        await using var context = _fixture.CreateContext();
        var readService = new UserRestrictionReadService(context);

        IReadOnlyList<UserRestrictionResponse> result =
            await readService.GetByUserIdAsync(userId, CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByUserIdAsync_Should_Return_AllAssociationsOfUser()
    {
        UserId userId = await SeedUserAsync();
        RestrictionId first = await SeedRestrictionAsync("Sin TACC");
        RestrictionId second = await SeedRestrictionAsync("Sin lactosa");

        await SeedAssociationAsync(userId, first, UserRestrictionImportanceLevels.High);
        await SeedAssociationAsync(userId, second, UserRestrictionImportanceLevels.Low);

        await using var context = _fixture.CreateContext();
        var readService = new UserRestrictionReadService(context);

        IReadOnlyList<UserRestrictionResponse> result =
            await readService.GetByUserIdAsync(userId, CancellationToken.None);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetByUserIdAsync_Should_Not_Return_AssociationsOfOtherUsers()
    {
        UserId userId = await SeedUserAsync();
        UserId otherUserId = await SeedUserAsync();
        RestrictionId restrictionId = await SeedRestrictionAsync("Sin TACC");

        await SeedAssociationAsync(
            otherUserId, restrictionId, UserRestrictionImportanceLevels.High);

        await using var context = _fixture.CreateContext();
        var readService = new UserRestrictionReadService(context);

        IReadOnlyList<UserRestrictionResponse> result =
            await readService.GetByUserIdAsync(userId, CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByIdsAsync_Should_Return_Association_WithRestrictionName()
    {
        UserId userId = await SeedUserAsync();
        RestrictionId restrictionId = await SeedRestrictionAsync("Sin TACC");
        await SeedAssociationAsync(userId, restrictionId, UserRestrictionImportanceLevels.Medium);

        await using var context = _fixture.CreateContext();
        var readService = new UserRestrictionReadService(context);

        UserRestrictionResponse? result = await readService.GetByIdsAsync(
            userId, restrictionId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Sin TACC", result.RestrictionName);
        Assert.Equal(UserRestrictionImportanceLevels.Medium, result.ImportanceLevel);
    }

    [Fact]
    public async Task GetByIdsAsync_Should_Return_Null_WhenAssociationDoesNotExist()
    {
        UserId userId = await SeedUserAsync();
        RestrictionId restrictionId = await SeedRestrictionAsync("Sin TACC");

        await using var context = _fixture.CreateContext();
        var readService = new UserRestrictionReadService(context);

        UserRestrictionResponse? result = await readService.GetByIdsAsync(
            userId, restrictionId, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdsAsync_Should_Return_Null_WhenAssociationBelongsToAnotherUser()
    {
        UserId userId = await SeedUserAsync();
        UserId otherUserId = await SeedUserAsync();
        RestrictionId restrictionId = await SeedRestrictionAsync("Sin TACC");

        await SeedAssociationAsync(
            otherUserId, restrictionId, UserRestrictionImportanceLevels.High);

        await using var context = _fixture.CreateContext();
        var readService = new UserRestrictionReadService(context);

        UserRestrictionResponse? result = await readService.GetByIdsAsync(
            userId, restrictionId, CancellationToken.None);

        Assert.Null(result);
    }

    public void Dispose()
    {
        _fixture.Dispose();
    }
}
