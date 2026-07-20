using Lyria.Domain.Restrictions;
using Lyria.Infrastructure.Persistence;
using Lyria.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Lyria.Infrastructure.IntegrationTests.Persistence;

public sealed class RestrictionRepositoryTests : IDisposable
{
    private readonly SqliteFixture _fixture = new();

    [Fact]
    public async Task AddAsync_And_SaveChanges_PersistsRestriction()
    {
        using var context = _fixture.CreateContext();
        var repository = new RestrictionRepository(context);

        var restriction = Restriction.Create(RestrictionId.New(), "Vegano", "Sin productos de origen animal.");

        await repository.AddAsync(restriction, TestContext.Current.CancellationToken);
        await repository.SaveChangesAsync(TestContext.Current.CancellationToken);

        using var readContext = _fixture.CreateContext();
        var persisted = await readContext.Set<Restriction>().FindAsync([restriction.Id], TestContext.Current.CancellationToken);

        Assert.NotNull(persisted);
        Assert.Equal("Vegano", persisted.Name);
        Assert.Equal("Sin productos de origen animal.", persisted.Description);
        Assert.True(persisted.IsActive);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsRestriction()
    {
        using var context = _fixture.CreateContext();
        var repository = new RestrictionRepository(context);

        var restriction = Restriction.Create(RestrictionId.New(), "Vegetariano", null);
        await repository.AddAsync(restriction, TestContext.Current.CancellationToken);
        await repository.SaveChangesAsync(TestContext.Current.CancellationToken);

        var found = await repository.GetByIdAsync(restriction.Id, TestContext.Current.CancellationToken);

        Assert.NotNull(found);
        Assert.Equal(restriction.Id, found.Id);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentId_ReturnsNull()
    {
        using var context = _fixture.CreateContext();
        var repository = new RestrictionRepository(context);

        var found = await repository.GetByIdAsync(RestrictionId.New(), TestContext.Current.CancellationToken);

        Assert.Null(found);
    }

    [Fact]
    public async Task ExistsByNameAsync_ExistingName_ReturnsTrue()
    {
        using var context = _fixture.CreateContext();
        var repository = new RestrictionRepository(context);

        var restriction = Restriction.Create(RestrictionId.New(), "Sin TACC", null);
        await repository.AddAsync(restriction, TestContext.Current.CancellationToken);
        await repository.SaveChangesAsync(TestContext.Current.CancellationToken);

        bool exists = await repository.ExistsByNameAsync("Sin TACC", excludingId: null, TestContext.Current.CancellationToken);

        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsByNameAsync_ExcludingSameId_ReturnsFalse()
    {
        using var context = _fixture.CreateContext();
        var repository = new RestrictionRepository(context);

        var restriction = Restriction.Create(RestrictionId.New(), "Sin lactosa", null);
        await repository.AddAsync(restriction, TestContext.Current.CancellationToken);
        await repository.SaveChangesAsync(TestContext.Current.CancellationToken);

        bool exists = await repository.ExistsByNameAsync("Sin lactosa", excludingId: restriction.Id, TestContext.Current.CancellationToken);

        Assert.False(exists);
    }

    [Fact]
    public async Task ExistsByNameAsync_NonExistentName_ReturnsFalse()
    {
        using var context = _fixture.CreateContext();
        var repository = new RestrictionRepository(context);

        bool exists = await repository.ExistsByNameAsync("No existe", excludingId: null, TestContext.Current.CancellationToken);

        Assert.False(exists);
    }

    [Fact]
    public async Task ExistsByNameAsync_DifferentCase_ReturnsTrue()
    {
        using var context = _fixture.CreateContext();
        var repository = new RestrictionRepository(context);

        var restriction = Restriction.Create(RestrictionId.New(), "Sin TACC", null);
        await repository.AddAsync(restriction, TestContext.Current.CancellationToken);
        await repository.SaveChangesAsync(TestContext.Current.CancellationToken);

        bool exists = await repository.ExistsByNameAsync("sin tacc", excludingId: null, TestContext.Current.CancellationToken);

        Assert.True(exists);
    }

    public void Dispose() => _fixture.Dispose();
}
