using Lyria.Application.Features.Establishments;
using Lyria.Domain.Establishments;
using Lyria.Domain.Establishments.Categories;
using Lyria.Infrastructure.Persistence.ReadServices;
using Xunit;

namespace Lyria.Infrastructure.IntegrationTests.Persistence;

public sealed class EstablishmentReadServiceTests : IDisposable
{
    private readonly SqliteFixture _fixture = new();

    private async Task<EstablishmentCategoryId> SeedCategoryAsync(string name = "Restaurante")
    {
        var categoryId = EstablishmentCategoryId.New();
        await using var context = _fixture.CreateContext();
        var category = EstablishmentCategory.Create(categoryId, name, null, null, 1);
        context.Set<EstablishmentCategory>().Add(category);
        await context.SaveChangesAsync(CancellationToken.None);
        return categoryId;
    }

    private async Task SeedEstablishmentAsync(
        EstablishmentCategoryId categoryId,
        string name = "Let It V",
        string slug = "let-it-v",
        string? description = null,
        string? website = null,
        string? instagram = null,
        bool verify = false,
        bool isActive = true)
    {
        var establishment = Establishment.Create(
            EstablishmentId.New(), categoryId, name, slug, description, website, instagram, null, null, null);
        if (verify)
        {
            establishment.Verify(new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc));
        }

        if (!isActive)
        {
            establishment.Deactivate();
        }

        await using var context = _fixture.CreateContext();
        context.Set<Establishment>().Add(establishment);
        await context.SaveChangesAsync(CancellationToken.None);
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Establishment_With_All_Fields()
    {
        var categoryId = await SeedCategoryAsync();
        var establishment = Establishment.Create(
            EstablishmentId.New(), categoryId, "Let It V", "let-it-v",
            "Vegano", "https://letitv.com", "@letitv", null, null, null);

        await using (var context = _fixture.CreateContext())
        {
            context.Set<Establishment>().Add(establishment);
            await context.SaveChangesAsync(CancellationToken.None);
        }

        await using (var context = _fixture.CreateContext())
        {
            var readService = new EstablishmentReadService(context);
            var result = await readService.GetByIdAsync(establishment.Id, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(establishment.Id.Value, result.Id);
            Assert.Equal(categoryId.Value, result.CategoryId);
            Assert.Equal("Restaurante", result.CategoryName);
            Assert.Equal("Let It V", result.Name);
            Assert.Equal("let-it-v", result.Slug);
            Assert.Equal("Vegano", result.Description);
            Assert.Equal("https://letitv.com", result.Website);
            Assert.Equal("@letitv", result.Instagram);
            Assert.False(result.IsVerified);
            Assert.True(result.IsActive);
        }
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Null_For_NonExistent_Id()
    {
        await using var context = _fixture.CreateContext();
        var readService = new EstablishmentReadService(context);

        var result = await readService.GetByIdAsync(EstablishmentId.New(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task ListAsync_Should_Return_All_Establishments()
    {
        var categoryId = await SeedCategoryAsync();
        await SeedEstablishmentAsync(categoryId, "Alpha", "alpha");
        await SeedEstablishmentAsync(categoryId, "Beta", "beta");

        await using var context = _fixture.CreateContext();
        var readService = new EstablishmentReadService(context);
        var filter = new EstablishmentListFilter(null, null, null, null, 1, 20);

        var result = await readService.ListAsync(filter, CancellationToken.None);

        Assert.Equal(2, result.TotalItems);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task ListAsync_Should_Order_By_Name()
    {
        var categoryId = await SeedCategoryAsync();
        await SeedEstablishmentAsync(categoryId, "Charlie", "charlie");
        await SeedEstablishmentAsync(categoryId, "Alpha", "alpha");
        await SeedEstablishmentAsync(categoryId, "Beta", "beta");

        await using var context = _fixture.CreateContext();
        var readService = new EstablishmentReadService(context);
        var filter = new EstablishmentListFilter(null, null, null, null, 1, 20);

        var result = await readService.ListAsync(filter, CancellationToken.None);

        Assert.Equal("Alpha", result.Items[0].Name);
        Assert.Equal("Beta", result.Items[1].Name);
        Assert.Equal("Charlie", result.Items[2].Name);
    }

    [Fact]
    public async Task ListAsync_Should_Filter_By_Status()
    {
        var categoryId = await SeedCategoryAsync();
        await SeedEstablishmentAsync(categoryId, "Active One", "active-one");
        await SeedEstablishmentAsync(categoryId, "Inactive One", "inactive-one",
            isActive: false);

        await using var context = _fixture.CreateContext();
        var readService = new EstablishmentReadService(context);
        var filter = new EstablishmentListFilter(null, null, true, null, 1, 20);

        var result = await readService.ListAsync(filter, CancellationToken.None);

        Assert.Equal(1, result.TotalItems);
        Assert.Equal("Active One", result.Items[0].Name);
    }

    [Fact]
    public async Task ListAsync_Should_Filter_By_IsVerified()
    {
        var categoryId = await SeedCategoryAsync();
        await SeedEstablishmentAsync(categoryId, "Verified", "verified", verify: true);
        await SeedEstablishmentAsync(categoryId, "Not Verified", "not-verified");

        await using var context = _fixture.CreateContext();
        var readService = new EstablishmentReadService(context);
        var filter = new EstablishmentListFilter(null, null, null, true, 1, 20);

        var result = await readService.ListAsync(filter, CancellationToken.None);

        Assert.Equal(1, result.TotalItems);
        Assert.Equal("Verified", result.Items[0].Name);
    }

    [Fact]
    public async Task ListAsync_Should_Filter_By_Search()
    {
        var categoryId = await SeedCategoryAsync();
        await SeedEstablishmentAsync(categoryId, "Let It V", "let-it-v");
        await SeedEstablishmentAsync(categoryId, "Cafe Zen", "cafe-zen");

        await using var context = _fixture.CreateContext();
        var readService = new EstablishmentReadService(context);
        var filter = new EstablishmentListFilter("Let", null, null, null, 1, 20);

        var result = await readService.ListAsync(filter, CancellationToken.None);

        Assert.Equal(1, result.TotalItems);
        Assert.Equal("Let It V", result.Items[0].Name);
    }

    [Fact]
    public async Task ListAsync_Should_Paginate()
    {
        var categoryId = await SeedCategoryAsync();
        await SeedEstablishmentAsync(categoryId, "Alpha", "alpha");
        await SeedEstablishmentAsync(categoryId, "Beta", "beta");
        await SeedEstablishmentAsync(categoryId, "Charlie", "charlie");

        await using var context = _fixture.CreateContext();
        var readService = new EstablishmentReadService(context);
        var filter = new EstablishmentListFilter(null, null, null, null, 1, 2);

        var result = await readService.ListAsync(filter, CancellationToken.None);

        Assert.Equal(3, result.TotalItems);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(2, result.TotalPages);
    }

    [Fact]
    public async Task ListAsync_Should_Return_Empty_When_No_Data()
    {
        await using var context = _fixture.CreateContext();
        var readService = new EstablishmentReadService(context);
        var filter = new EstablishmentListFilter(null, null, null, null, 1, 20);

        var result = await readService.ListAsync(filter, CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalItems);
    }

    [Fact]
    public async Task Queries_Should_Use_AsNoTracking()
    {
        var categoryId = await SeedCategoryAsync();
        await SeedEstablishmentAsync(categoryId);

        await using var context = _fixture.CreateContext();
        var readService = new EstablishmentReadService(context);

        await readService.ListAsync(
            new EstablishmentListFilter(null, null, null, null, 1, 20),
            CancellationToken.None);

        var trackedEntries = context.ChangeTracker.Entries<Establishment>().ToList();
        Assert.Empty(trackedEntries);
    }

    public void Dispose()
    {
        _fixture.Dispose();
    }
}
