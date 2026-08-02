using Lyria.Domain.Restrictions;
using Lyria.Domain.Users;
using Lyria.Domain.Users.UserRestrictions;
using Lyria.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Lyria.Infrastructure.IntegrationTests.Persistence.UserRestrictions;

public sealed class UserRestrictionRepositoryTests : IDisposable
{
    private static readonly DateTime CreatedAtUtc =
        new(2026, 8, 2, 5, 0, 0, DateTimeKind.Utc);

    private readonly SqliteFixture _fixture = new();

    private async Task<(UserId UserId, RestrictionId RestrictionId)> SeedPrincipalsAsync()
    {
        var user = User.Create(
            UserId.New(), "Juan", "Garcia", $"{Guid.NewGuid():N}@example.com",
            "hashed_pw", null, null, null);

        var restriction = Restriction.Create(RestrictionId.New(), "Sin TACC", null);

        await using var context = _fixture.CreateContext();
        context.Set<User>().Add(user);
        context.Set<Restriction>().Add(restriction);
        await context.SaveChangesAsync(CancellationToken.None);

        return (user.Id, restriction.Id);
    }

    private async Task AddAssociationAsync(
        UserId userId,
        RestrictionId restrictionId,
        string importanceLevel)
    {
        await using var context = _fixture.CreateContext();
        var repository = new UserRestrictionRepository(context);

        repository.Add(UserRestriction.Create(
            userId, restrictionId, importanceLevel, CreatedAtUtc));

        await repository.SaveChangesAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Add_And_SaveChanges_Should_Persist_Association()
    {
        (UserId userId, RestrictionId restrictionId) = await SeedPrincipalsAsync();

        await AddAssociationAsync(userId, restrictionId, UserRestrictionImportanceLevels.High);

        await using var context = _fixture.CreateContext();
        UserRestriction? persisted = await context.Set<UserRestriction>()
            .FirstOrDefaultAsync(
                ur => ur.UserId == userId && ur.RestrictionId == restrictionId,
                CancellationToken.None);

        Assert.NotNull(persisted);
        Assert.Equal(userId, persisted.UserId);
        Assert.Equal(restrictionId, persisted.RestrictionId);
        Assert.Equal(UserRestrictionImportanceLevels.High, persisted.ImportanceLevel);
        Assert.Equal(CreatedAtUtc, persisted.CreatedAtUtc);
    }

    [Theory]
    [InlineData("Low")]
    [InlineData("Medium")]
    [InlineData("High")]
    public async Task Add_Should_Persist_EveryAuthorizedImportanceLevel(string importanceLevel)
    {
        (UserId userId, RestrictionId restrictionId) = await SeedPrincipalsAsync();

        await AddAssociationAsync(userId, restrictionId, importanceLevel);

        await using var context = _fixture.CreateContext();
        UserRestriction? persisted = await context.Set<UserRestriction>()
            .FirstOrDefaultAsync(
                ur => ur.UserId == userId && ur.RestrictionId == restrictionId,
                CancellationToken.None);

        Assert.NotNull(persisted);
        Assert.Equal(importanceLevel, persisted.ImportanceLevel);
    }

    [Fact]
    public async Task Add_DuplicateAssociation_Should_Be_Rejected_ByDatabase()
    {
        (UserId userId, RestrictionId restrictionId) = await SeedPrincipalsAsync();

        await AddAssociationAsync(userId, restrictionId, UserRestrictionImportanceLevels.Low);

        await Assert.ThrowsAsync<DbUpdateException>(async () =>
            await AddAssociationAsync(
                userId, restrictionId, UserRestrictionImportanceLevels.High));
    }

    [Fact]
    public async Task Add_WithNonExistentUser_Should_Be_Rejected_ByForeignKey()
    {
        (_, RestrictionId restrictionId) = await SeedPrincipalsAsync();

        await Assert.ThrowsAsync<DbUpdateException>(async () =>
            await AddAssociationAsync(
                UserId.New(), restrictionId, UserRestrictionImportanceLevels.High));
    }

    [Fact]
    public async Task Add_WithNonExistentRestriction_Should_Be_Rejected_ByForeignKey()
    {
        (UserId userId, _) = await SeedPrincipalsAsync();

        await Assert.ThrowsAsync<DbUpdateException>(async () =>
            await AddAssociationAsync(
                userId, RestrictionId.New(), UserRestrictionImportanceLevels.High));
    }

    [Fact]
    public async Task GetByIdsAsync_Should_Return_Persisted_Association()
    {
        (UserId userId, RestrictionId restrictionId) = await SeedPrincipalsAsync();
        await AddAssociationAsync(userId, restrictionId, UserRestrictionImportanceLevels.Medium);

        await using var context = _fixture.CreateContext();
        var repository = new UserRestrictionRepository(context);

        UserRestriction? result = await repository.GetByIdsAsync(
            userId, restrictionId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(UserRestrictionImportanceLevels.Medium, result.ImportanceLevel);
    }

    [Fact]
    public async Task GetByIdsAsync_Should_Return_Null_WhenNotFound()
    {
        await using var context = _fixture.CreateContext();
        var repository = new UserRestrictionRepository(context);

        UserRestriction? result = await repository.GetByIdsAsync(
            UserId.New(), RestrictionId.New(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task ExistsAsync_Should_Return_True_WhenAssociationExists()
    {
        (UserId userId, RestrictionId restrictionId) = await SeedPrincipalsAsync();
        await AddAssociationAsync(userId, restrictionId, UserRestrictionImportanceLevels.Low);

        await using var context = _fixture.CreateContext();
        var repository = new UserRestrictionRepository(context);

        Assert.True(await repository.ExistsAsync(
            userId, restrictionId, CancellationToken.None));
    }

    [Fact]
    public async Task ExistsAsync_Should_Return_False_WhenAssociationDoesNotExist()
    {
        await using var context = _fixture.CreateContext();
        var repository = new UserRestrictionRepository(context);

        Assert.False(await repository.ExistsAsync(
            UserId.New(), RestrictionId.New(), CancellationToken.None));
    }

    [Fact]
    public async Task UpdateImportanceLevel_Should_Persist_And_Preserve_CreatedAtUtc()
    {
        (UserId userId, RestrictionId restrictionId) = await SeedPrincipalsAsync();
        await AddAssociationAsync(userId, restrictionId, UserRestrictionImportanceLevels.High);

        await using (var context = _fixture.CreateContext())
        {
            var repository = new UserRestrictionRepository(context);
            UserRestriction association = (await repository.GetByIdsAsync(
                userId, restrictionId, CancellationToken.None))!;

            association.UpdateImportanceLevel(UserRestrictionImportanceLevels.Low);
            await repository.SaveChangesAsync(CancellationToken.None);
        }

        await using (var context = _fixture.CreateContext())
        {
            UserRestriction persisted = (await context.Set<UserRestriction>()
                .FirstOrDefaultAsync(
                    ur => ur.UserId == userId && ur.RestrictionId == restrictionId,
                    CancellationToken.None))!;

            Assert.Equal(UserRestrictionImportanceLevels.Low, persisted.ImportanceLevel);
            Assert.Equal(CreatedAtUtc, persisted.CreatedAtUtc);
        }
    }

    [Fact]
    public async Task Remove_Should_Delete_OnlyTheAssociation()
    {
        (UserId userId, RestrictionId restrictionId) = await SeedPrincipalsAsync();
        await AddAssociationAsync(userId, restrictionId, UserRestrictionImportanceLevels.High);

        await using (var context = _fixture.CreateContext())
        {
            var repository = new UserRestrictionRepository(context);
            UserRestriction association = (await repository.GetByIdsAsync(
                userId, restrictionId, CancellationToken.None))!;

            repository.Remove(association);
            await repository.SaveChangesAsync(CancellationToken.None);
        }

        await using (var context = _fixture.CreateContext())
        {
            Assert.Empty(await context.Set<UserRestriction>()
                .ToListAsync(CancellationToken.None));

            Assert.NotNull(await context.Set<User>()
                .FirstOrDefaultAsync(u => u.Id == userId, CancellationToken.None));

            Assert.NotNull(await context.Set<Restriction>()
                .FirstOrDefaultAsync(r => r.Id == restrictionId, CancellationToken.None));
        }
    }

    [Fact]
    public async Task Delete_User_WithAssociation_Should_Be_Restricted()
    {
        (UserId userId, RestrictionId restrictionId) = await SeedPrincipalsAsync();
        await AddAssociationAsync(userId, restrictionId, UserRestrictionImportanceLevels.High);

        await using var context = _fixture.CreateContext();
        User user = (await context.Set<User>()
            .FirstOrDefaultAsync(u => u.Id == userId, CancellationToken.None))!;

        context.Set<User>().Remove(user);

        await Assert.ThrowsAsync<DbUpdateException>(async () =>
            await context.SaveChangesAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Delete_Restriction_WithAssociation_Should_Be_Restricted()
    {
        (UserId userId, RestrictionId restrictionId) = await SeedPrincipalsAsync();
        await AddAssociationAsync(userId, restrictionId, UserRestrictionImportanceLevels.High);

        await using var context = _fixture.CreateContext();
        Restriction restriction = (await context.Set<Restriction>()
            .FirstOrDefaultAsync(r => r.Id == restrictionId, CancellationToken.None))!;

        context.Set<Restriction>().Remove(restriction);

        await Assert.ThrowsAsync<DbUpdateException>(async () =>
            await context.SaveChangesAsync(CancellationToken.None));
    }

    public void Dispose()
    {
        _fixture.Dispose();
    }
}
