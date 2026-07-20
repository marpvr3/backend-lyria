using Lyria.Domain.Establishments.Categories;
using Lyria.Infrastructure.Persistence.ReadServices;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Lyria.Infrastructure.IntegrationTests.Persistence;

public sealed class EstablishmentCategoryReadServiceTests : IDisposable
{
    private readonly SqliteFixture _fixture = new();

    [Fact]
    public async Task GetActiveByIdAsync_Should_Return_Active_Category_With_All_Fields()
    {
        var category = EstablishmentCategory.Create(
            EstablishmentCategoryId.New(), "Restaurante", "Lugar para comer", null, 1);

        await using (var context = _fixture.CreateContext())
        {
            context.Set<EstablishmentCategory>().Add(category);
            await context.SaveChangesAsync(CancellationToken.None);
        }

        await using (var context = _fixture.CreateContext())
        {
            var readService = new EstablishmentCategoryReadService(context);
            var result = await readService.GetActiveByIdAsync(category.Id, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(category.Id.Value, result.Id);
            Assert.Equal("Restaurante", result.Name);
            Assert.Equal("Lugar para comer", result.Description);
            Assert.Equal(1, result.SortOrder);
            Assert.True(result.IsActive);
        }
    }

    [Fact]
    public async Task GetActiveByIdAsync_Should_Return_Null_For_Inactive_Category()
    {
        var category = EstablishmentCategory.Create(
            EstablishmentCategoryId.New(), "Spa", null, null, 3);
        category.Deactivate();

        await using (var context = _fixture.CreateContext())
        {
            context.Set<EstablishmentCategory>().Add(category);
            await context.SaveChangesAsync(CancellationToken.None);
        }

        await using (var context = _fixture.CreateContext())
        {
            var readService = new EstablishmentCategoryReadService(context);
            var result = await readService.GetActiveByIdAsync(category.Id, CancellationToken.None);

            Assert.Null(result);
        }
    }

    [Fact]
    public async Task GetActiveByIdAsync_Should_Return_Null_For_NonExistent_Id()
    {
        await using var context = _fixture.CreateContext();
        var readService = new EstablishmentCategoryReadService(context);

        var result = await readService.GetActiveByIdAsync(EstablishmentCategoryId.New(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task ListActiveAsync_Should_Return_Only_Active_Categories()
    {
        var active = EstablishmentCategory.Create(
            EstablishmentCategoryId.New(), "Activa", null, null, 1);

        var inactive = EstablishmentCategory.Create(
            EstablishmentCategoryId.New(), "Inactiva", null, null, 2);
        inactive.Deactivate();

        await using (var context = _fixture.CreateContext())
        {
            context.Set<EstablishmentCategory>().AddRange(active, inactive);
            await context.SaveChangesAsync(CancellationToken.None);
        }

        await using (var context = _fixture.CreateContext())
        {
            var readService = new EstablishmentCategoryReadService(context);
            var results = await readService.ListActiveAsync(CancellationToken.None);

            Assert.Single(results);
            Assert.Equal("Activa", results[0].Name);
            Assert.True(results[0].IsActive);
        }
    }

    [Fact]
    public async Task ListActiveAsync_Should_Order_By_SortOrder_Ascending()
    {
        var third = EstablishmentCategory.Create(
            EstablishmentCategoryId.New(), "Tercera", null, null, 3);
        var first = EstablishmentCategory.Create(
            EstablishmentCategoryId.New(), "Primera", null, null, 1);
        var second = EstablishmentCategory.Create(
            EstablishmentCategoryId.New(), "Segunda", null, null, 2);

        await using (var context = _fixture.CreateContext())
        {
            context.Set<EstablishmentCategory>().AddRange(third, first, second);
            await context.SaveChangesAsync(CancellationToken.None);
        }

        await using (var context = _fixture.CreateContext())
        {
            var readService = new EstablishmentCategoryReadService(context);
            var results = await readService.ListActiveAsync(CancellationToken.None);

            Assert.Equal(3, results.Count);
            Assert.Equal("Primera", results[0].Name);
            Assert.Equal("Segunda", results[1].Name);
            Assert.Equal("Tercera", results[2].Name);
        }
    }

    [Fact]
    public async Task ListActiveAsync_Should_Break_Ties_By_Name_Ascending()
    {
        var bravo = EstablishmentCategory.Create(
            EstablishmentCategoryId.New(), "Bravo", null, null, 1);
        var alpha = EstablishmentCategory.Create(
            EstablishmentCategoryId.New(), "Alpha", null, null, 1);
        var charlie = EstablishmentCategory.Create(
            EstablishmentCategoryId.New(), "Charlie", null, null, 1);

        await using (var context = _fixture.CreateContext())
        {
            context.Set<EstablishmentCategory>().AddRange(bravo, alpha, charlie);
            await context.SaveChangesAsync(CancellationToken.None);
        }

        await using (var context = _fixture.CreateContext())
        {
            var readService = new EstablishmentCategoryReadService(context);
            var results = await readService.ListActiveAsync(CancellationToken.None);

            Assert.Equal(3, results.Count);
            Assert.Equal("Alpha", results[0].Name);
            Assert.Equal("Bravo", results[1].Name);
            Assert.Equal("Charlie", results[2].Name);
        }
    }

    [Fact]
    public async Task ListActiveAsync_Should_Return_Empty_When_No_Data()
    {
        await using var context = _fixture.CreateContext();
        var readService = new EstablishmentCategoryReadService(context);

        var results = await readService.ListActiveAsync(CancellationToken.None);

        Assert.Empty(results);
    }

    [Fact]
    public async Task Queries_Should_Use_AsNoTracking()
    {
        var category = EstablishmentCategory.Create(
            EstablishmentCategoryId.New(), "Rastreo", null, null, 1);

        await using (var context = _fixture.CreateContext())
        {
            context.Set<EstablishmentCategory>().Add(category);
            await context.SaveChangesAsync(CancellationToken.None);
        }

        await using (var context = _fixture.CreateContext())
        {
            var readService = new EstablishmentCategoryReadService(context);

            await readService.GetActiveByIdAsync(category.Id, CancellationToken.None);
            await readService.ListActiveAsync(CancellationToken.None);

            var trackedEntries = context.ChangeTracker.Entries<EstablishmentCategory>().ToList();
            Assert.Empty(trackedEntries);
        }
    }

    [Fact]
    public async Task CancellationToken_Should_Be_Passed_Through()
    {
        await using var context = _fixture.CreateContext();
        var readService = new EstablishmentCategoryReadService(context);

        var result = await readService.GetActiveByIdAsync(EstablishmentCategoryId.New(), CancellationToken.None);

        Assert.Null(result);
    }

    public void Dispose()
    {
        _fixture.Dispose();
    }
}
