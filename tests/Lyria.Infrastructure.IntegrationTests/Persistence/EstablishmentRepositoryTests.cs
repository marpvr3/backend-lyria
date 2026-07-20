using Lyria.Domain.Establishments;
using Lyria.Domain.Establishments.Categories;
using Lyria.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Lyria.Infrastructure.IntegrationTests.Persistence;

public sealed class EstablishmentRepositoryTests : IDisposable
{
    private readonly SqliteFixture _fixture = new();

    private async Task SeedCategoryAsync(EstablishmentCategoryId categoryId)
    {
        await using var context = _fixture.CreateContext();
        var category = EstablishmentCategory.Create(
            categoryId, "Restaurante", null, null, 1);
        context.Set<EstablishmentCategory>().Add(category);
        await context.SaveChangesAsync(CancellationToken.None);
    }

    [Fact]
    public async Task AddAsync_And_SaveChangesAsync_Should_Persist_Establishment()
    {
        var categoryId = EstablishmentCategoryId.New();
        await SeedCategoryAsync(categoryId);

        var establishment = Establishment.Create(
            EstablishmentId.New(), categoryId, "Let It V", "let-it-v",
            "Vegano", "https://letitv.com", "@letitv", null, null, null);

        await using (var context = _fixture.CreateContext())
        {
            var repository = new EstablishmentRepository(context);
            await repository.AddAsync(establishment, CancellationToken.None);
            await repository.SaveChangesAsync(CancellationToken.None);
        }

        await using (var context = _fixture.CreateContext())
        {
            var persisted = await context.Set<Establishment>()
                .FirstOrDefaultAsync(e => e.Id == establishment.Id, CancellationToken.None);

            Assert.NotNull(persisted);
            Assert.Equal("Let It V", persisted.Name);
            Assert.Equal("let-it-v", persisted.Slug);
            Assert.Equal("Vegano", persisted.Description);
            Assert.Equal("https://letitv.com", persisted.Website);
            Assert.Equal("@letitv", persisted.Instagram);
            Assert.False(persisted.IsVerified);
            Assert.True(persisted.IsActive);
        }
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Persisted_Establishment()
    {
        var categoryId = EstablishmentCategoryId.New();
        await SeedCategoryAsync(categoryId);

        var establishment = Establishment.Create(
            EstablishmentId.New(), categoryId, "Cafe Zen", "cafe-zen",
            null, null, null, null, null, null);

        await using (var context = _fixture.CreateContext())
        {
            var repository = new EstablishmentRepository(context);
            await repository.AddAsync(establishment, CancellationToken.None);
            await repository.SaveChangesAsync(CancellationToken.None);
        }

        await using (var context = _fixture.CreateContext())
        {
            var repository = new EstablishmentRepository(context);
            var result = await repository.GetByIdAsync(establishment.Id, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(establishment.Id, result.Id);
            Assert.Equal("Cafe Zen", result.Name);
        }
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Null_For_NonExistent_Id()
    {
        await using var context = _fixture.CreateContext();
        var repository = new EstablishmentRepository(context);

        var result = await repository.GetByIdAsync(
            EstablishmentId.New(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task ExistsBySlugAsync_Should_Return_True_For_Existing_Slug()
    {
        var categoryId = EstablishmentCategoryId.New();
        await SeedCategoryAsync(categoryId);

        var establishment = Establishment.Create(
            EstablishmentId.New(), categoryId, "Test", "my-slug", null, null, null, null, null, null);

        await using (var context = _fixture.CreateContext())
        {
            var repository = new EstablishmentRepository(context);
            await repository.AddAsync(establishment, CancellationToken.None);
            await repository.SaveChangesAsync(CancellationToken.None);
        }

        await using (var context = _fixture.CreateContext())
        {
            var repository = new EstablishmentRepository(context);
            var exists = await repository.ExistsBySlugAsync(
                "my-slug", null, CancellationToken.None);

            Assert.True(exists);
        }
    }

    [Fact]
    public async Task ExistsBySlugAsync_Should_Exclude_Specified_Id()
    {
        var categoryId = EstablishmentCategoryId.New();
        await SeedCategoryAsync(categoryId);

        var establishment = Establishment.Create(
            EstablishmentId.New(), categoryId, "Test", "my-slug", null, null, null, null, null, null);

        await using (var context = _fixture.CreateContext())
        {
            var repository = new EstablishmentRepository(context);
            await repository.AddAsync(establishment, CancellationToken.None);
            await repository.SaveChangesAsync(CancellationToken.None);
        }

        await using (var context = _fixture.CreateContext())
        {
            var repository = new EstablishmentRepository(context);
            var exists = await repository.ExistsBySlugAsync(
                "my-slug", establishment.Id, CancellationToken.None);

            Assert.False(exists);
        }
    }

    [Fact]
    public async Task ExistsBySlugAsync_Should_Return_False_For_NonExistent_Slug()
    {
        await using var context = _fixture.CreateContext();
        var repository = new EstablishmentRepository(context);

        var exists = await repository.ExistsBySlugAsync(
            "nonexistent", null, CancellationToken.None);

        Assert.False(exists);
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Tracked_Entity()
    {
        var categoryId = EstablishmentCategoryId.New();
        await SeedCategoryAsync(categoryId);

        var establishment = Establishment.Create(
            EstablishmentId.New(), categoryId, "Test", "tracked-test", null, null, null, null, null, null);

        await using (var context = _fixture.CreateContext())
        {
            var repository = new EstablishmentRepository(context);
            await repository.AddAsync(establishment, CancellationToken.None);
            await repository.SaveChangesAsync(CancellationToken.None);
        }

        await using (var context = _fixture.CreateContext())
        {
            var repository = new EstablishmentRepository(context);
            var result = await repository.GetByIdAsync(establishment.Id, CancellationToken.None);

            Assert.NotNull(result);
            var trackedEntries = context.ChangeTracker.Entries<Establishment>().ToList();
            Assert.Single(trackedEntries);
            Assert.Equal(EntityState.Unchanged, trackedEntries[0].State);
        }
    }

    public void Dispose()
    {
        _fixture.Dispose();
    }
}
