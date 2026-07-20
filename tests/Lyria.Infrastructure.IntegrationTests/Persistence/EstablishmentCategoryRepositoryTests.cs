using Lyria.Domain.Establishments.Categories;
using Lyria.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Lyria.Infrastructure.IntegrationTests.Persistence;

public sealed class EstablishmentCategoryRepositoryTests : IDisposable
{
    private readonly SqliteFixture _fixture = new();

    [Fact]
    public async Task AddAsync_And_SaveChangesAsync_Should_Persist_Category()
    {
        var category = EstablishmentCategory.Create(
            EstablishmentCategoryId.New(), "Restaurante", "Descripción", null, 1);

        await using (var context = _fixture.CreateContext())
        {
            var repository = new EstablishmentCategoryRepository(context);
            await repository.AddAsync(category, CancellationToken.None);
            await repository.SaveChangesAsync(CancellationToken.None);
        }

        await using (var context = _fixture.CreateContext())
        {
            var persisted = await context.Set<EstablishmentCategory>()
                .FirstOrDefaultAsync(c => c.Id == category.Id, CancellationToken.None);

            Assert.NotNull(persisted);
            Assert.Equal("Restaurante", persisted.Name);
            Assert.Equal("Descripción", persisted.Description);
            Assert.Equal(1, persisted.SortOrder);
            Assert.True(persisted.IsActive);
        }
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Persisted_Category()
    {
        var category = EstablishmentCategory.Create(
            EstablishmentCategoryId.New(), "Hotel", "Un hotel", null, 2);

        await using (var context = _fixture.CreateContext())
        {
            var repository = new EstablishmentCategoryRepository(context);
            await repository.AddAsync(category, CancellationToken.None);
            await repository.SaveChangesAsync(CancellationToken.None);
        }

        await using (var context = _fixture.CreateContext())
        {
            var repository = new EstablishmentCategoryRepository(context);
            var result = await repository.GetByIdAsync(category.Id, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(category.Id, result.Id);
            Assert.Equal("Hotel", result.Name);
        }
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Null_For_NonExistent_Id()
    {
        await using var context = _fixture.CreateContext();
        var repository = new EstablishmentCategoryRepository(context);

        var result = await repository.GetByIdAsync(
            EstablishmentCategoryId.New(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task ExistsByNameAsync_Should_Return_True_For_Existing_Name()
    {
        var category = EstablishmentCategory.Create(
            EstablishmentCategoryId.New(), "Spa", null, null, 3);

        await using (var context = _fixture.CreateContext())
        {
            var repository = new EstablishmentCategoryRepository(context);
            await repository.AddAsync(category, CancellationToken.None);
            await repository.SaveChangesAsync(CancellationToken.None);
        }

        await using (var context = _fixture.CreateContext())
        {
            var repository = new EstablishmentCategoryRepository(context);
            var exists = await repository.ExistsByNameAsync("Spa", null, CancellationToken.None);

            Assert.True(exists);
        }
    }

    [Fact]
    public async Task ExistsByNameAsync_Should_Exclude_Specified_Id()
    {
        var category = EstablishmentCategory.Create(
            EstablishmentCategoryId.New(), "Bar", null, null, 4);

        await using (var context = _fixture.CreateContext())
        {
            var repository = new EstablishmentCategoryRepository(context);
            await repository.AddAsync(category, CancellationToken.None);
            await repository.SaveChangesAsync(CancellationToken.None);
        }

        await using (var context = _fixture.CreateContext())
        {
            var repository = new EstablishmentCategoryRepository(context);
            var exists = await repository.ExistsByNameAsync(
                "Bar", category.Id, CancellationToken.None);

            Assert.False(exists);
        }
    }

    [Fact]
    public async Task ExistsByNameAsync_Should_Return_False_For_NonExistent_Name()
    {
        await using var context = _fixture.CreateContext();
        var repository = new EstablishmentCategoryRepository(context);

        var exists = await repository.ExistsByNameAsync(
            "Inexistente", null, CancellationToken.None);

        Assert.False(exists);
    }

    [Fact]
    public async Task CancellationToken_Should_Be_Passed_Through()
    {
        await using var context = _fixture.CreateContext();
        var repository = new EstablishmentCategoryRepository(context);

        var result = await repository.GetByIdAsync(
            EstablishmentCategoryId.New(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Tracked_Entity()
    {
        var category = EstablishmentCategory.Create(
            EstablishmentCategoryId.New(), "Cafetería", null, null, 5);

        await using (var context = _fixture.CreateContext())
        {
            var repository = new EstablishmentCategoryRepository(context);
            await repository.AddAsync(category, CancellationToken.None);
            await repository.SaveChangesAsync(CancellationToken.None);
        }

        await using (var context = _fixture.CreateContext())
        {
            var repository = new EstablishmentCategoryRepository(context);
            var result = await repository.GetByIdAsync(category.Id, CancellationToken.None);

            Assert.NotNull(result);

            var trackedEntries = context.ChangeTracker.Entries<EstablishmentCategory>().ToList();
            Assert.Single(trackedEntries);
            Assert.Equal(EntityState.Unchanged, trackedEntries[0].State);
        }
    }

    public void Dispose()
    {
        _fixture.Dispose();
    }
}
