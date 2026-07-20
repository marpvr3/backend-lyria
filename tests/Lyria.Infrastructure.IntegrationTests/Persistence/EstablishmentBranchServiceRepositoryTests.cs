using Lyria.Domain.Establishments;
using Lyria.Domain.Establishments.Branches;
using Lyria.Domain.Establishments.Categories;
using Lyria.Domain.Services;
using Lyria.Infrastructure.Persistence;
using Lyria.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Lyria.Infrastructure.IntegrationTests.Persistence;

public sealed class EstablishmentBranchServiceRepositoryTests : IDisposable
{
    private readonly SqliteFixture _fixture = new();

    private static async Task<(EstablishmentBranch Branch, Service Service)> SeedDependencies(LyriaDbContext context)
    {
        var category = EstablishmentCategory.Create(
            EstablishmentCategoryId.New(), "Restaurante", null, null, 1);
        context.Set<EstablishmentCategory>().Add(category);

        var establishment = Establishment.Create(
            EstablishmentId.New(), category.Id, "Let It V", "let-it-v",
            null, null, null, null, null, null);
        context.Set<Establishment>().Add(establishment);

        var branch = EstablishmentBranch.Create(
            EstablishmentBranchId.New(), establishment.Id,
            "Sede Central", "Costa Rica 5865",
            null, null, null, null, null, null, null,
            null, null, null, null, null);
        context.Set<EstablishmentBranch>().Add(branch);

        var service = Service.Create(ServiceId.New(), "Delivery", "Entrega.", null);
        context.Set<Service>().Add(service);

        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (branch, service);
    }

    [Fact]
    public async Task Add_And_SaveChanges_PersistsEntity()
    {
        using var context = _fixture.CreateContext();
        var (branch, service) = await SeedDependencies(context);

        var repository = new EstablishmentBranchServiceRepository(context);
        var branchService = EstablishmentBranchService.Create(
            branch.Id, service.Id, true, "Observación de prueba");

        repository.Add(branchService);
        await repository.SaveChangesAsync(TestContext.Current.CancellationToken);

        using var readContext = _fixture.CreateContext();
        var persisted = await readContext.Set<EstablishmentBranchService>()
            .FirstOrDefaultAsync(x => x.BranchId == branch.Id && x.ServiceId == service.Id,
                TestContext.Current.CancellationToken);

        Assert.NotNull(persisted);
        Assert.True(persisted.IsAvailable);
        Assert.Equal("Observación de prueba", persisted.Observation);
        Assert.True(persisted.IsActive);
    }

    [Fact]
    public async Task GetByIdsAsync_Existing_ReturnsEntity()
    {
        using var context = _fixture.CreateContext();
        var (branch, service) = await SeedDependencies(context);

        var branchService = EstablishmentBranchService.Create(branch.Id, service.Id, true, null);
        context.Set<EstablishmentBranchService>().Add(branchService);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repository = new EstablishmentBranchServiceRepository(context);
        var found = await repository.GetByIdsAsync(branch.Id, service.Id,
            TestContext.Current.CancellationToken);

        Assert.NotNull(found);
        Assert.Equal(branch.Id, found.BranchId);
        Assert.Equal(service.Id, found.ServiceId);
    }

    [Fact]
    public async Task GetByIdsAsync_NonExistent_ReturnsNull()
    {
        using var context = _fixture.CreateContext();
        var repository = new EstablishmentBranchServiceRepository(context);

        var found = await repository.GetByIdsAsync(
            EstablishmentBranchId.New(), ServiceId.New(),
            TestContext.Current.CancellationToken);

        Assert.Null(found);
    }

    [Fact]
    public async Task ExistsAsync_Existing_ReturnsTrue()
    {
        using var context = _fixture.CreateContext();
        var (branch, service) = await SeedDependencies(context);

        var branchService = EstablishmentBranchService.Create(branch.Id, service.Id, true, null);
        context.Set<EstablishmentBranchService>().Add(branchService);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repository = new EstablishmentBranchServiceRepository(context);
        bool exists = await repository.ExistsAsync(branch.Id, service.Id,
            TestContext.Current.CancellationToken);

        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsAsync_NonExistent_ReturnsFalse()
    {
        using var context = _fixture.CreateContext();
        var repository = new EstablishmentBranchServiceRepository(context);

        bool exists = await repository.ExistsAsync(
            EstablishmentBranchId.New(), ServiceId.New(),
            TestContext.Current.CancellationToken);

        Assert.False(exists);
    }

    [Fact]
    public async Task DuplicateKey_ThrowsOnAdd()
    {
        using var context = _fixture.CreateContext();
        var (branch, service) = await SeedDependencies(context);

        var first = EstablishmentBranchService.Create(branch.Id, service.Id, true, null);
        context.Set<EstablishmentBranchService>().Add(first);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var duplicate = EstablishmentBranchService.Create(branch.Id, service.Id, false, null);

        Assert.Throws<InvalidOperationException>(
            () => context.Set<EstablishmentBranchService>().Add(duplicate));
    }

    [Fact]
    public async Task Audit_CreatedAtUtc_IsSet()
    {
        using var context = _fixture.CreateContext();
        var (branch, service) = await SeedDependencies(context);

        var branchService = EstablishmentBranchService.Create(branch.Id, service.Id, true, null);
        context.Set<EstablishmentBranchService>().Add(branchService);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        using var readContext = _fixture.CreateContext();
        var persisted = await readContext.Set<EstablishmentBranchService>()
            .FirstAsync(x => x.BranchId == branch.Id && x.ServiceId == service.Id,
                TestContext.Current.CancellationToken);

        Assert.NotEqual(default, persisted.CreatedAtUtc);
        Assert.Null(persisted.UpdatedAtUtc);
    }

    [Fact]
    public async Task Audit_UpdatedAtUtc_IsSetOnModification()
    {
        using var context = _fixture.CreateContext();
        var (branch, service) = await SeedDependencies(context);

        var branchService = EstablishmentBranchService.Create(branch.Id, service.Id, true, null);
        context.Set<EstablishmentBranchService>().Add(branchService);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        branchService.Update(false, "Modificado");
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        using var readContext = _fixture.CreateContext();
        var persisted = await readContext.Set<EstablishmentBranchService>()
            .FirstAsync(x => x.BranchId == branch.Id && x.ServiceId == service.Id,
                TestContext.Current.CancellationToken);

        Assert.NotNull(persisted.UpdatedAtUtc);
    }

    public void Dispose() => _fixture.Dispose();
}
