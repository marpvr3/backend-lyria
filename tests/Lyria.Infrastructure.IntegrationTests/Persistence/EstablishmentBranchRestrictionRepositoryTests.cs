using Lyria.Domain.Establishments;
using Lyria.Domain.Establishments.Branches;
using Lyria.Domain.Establishments.Categories;
using Lyria.Domain.Restrictions;
using Lyria.Infrastructure.Persistence;
using Lyria.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Lyria.Infrastructure.IntegrationTests.Persistence;

public sealed class EstablishmentBranchRestrictionRepositoryTests : IDisposable
{
    private readonly SqliteFixture _fixture = new();

    private static async Task<(EstablishmentBranch Branch, Restriction Restriction)> SeedDependencies(LyriaDbContext context)
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

        var restriction = Restriction.Create(RestrictionId.New(), "Vegano", "Sin productos animales.");
        context.Set<Restriction>().Add(restriction);

        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (branch, restriction);
    }

    [Fact]
    public async Task Add_And_SaveChanges_PersistsEntity()
    {
        using var context = _fixture.CreateContext();
        var (branch, restriction) = await SeedDependencies(context);

        var repository = new EstablishmentBranchRestrictionRepository(context);
        var branchRestriction = EstablishmentBranchRestriction.Create(
            branch.Id, restriction.Id, RestrictionComplianceLevel.Guaranteed, true, "Observación de prueba");

        repository.Add(branchRestriction);
        await repository.SaveChangesAsync(TestContext.Current.CancellationToken);

        using var readContext = _fixture.CreateContext();
        var persisted = await readContext.Set<EstablishmentBranchRestriction>()
            .FirstOrDefaultAsync(x => x.BranchId == branch.Id && x.RestrictionId == restriction.Id,
                TestContext.Current.CancellationToken);

        Assert.NotNull(persisted);
        Assert.Equal(RestrictionComplianceLevel.Guaranteed, persisted.ComplianceLevel);
        Assert.True(persisted.IsCertified);
        Assert.Equal("Observación de prueba", persisted.Observation);
        Assert.True(persisted.IsActive);
    }

    [Fact]
    public async Task GetByIdsAsync_Existing_ReturnsEntity()
    {
        using var context = _fixture.CreateContext();
        var (branch, restriction) = await SeedDependencies(context);

        var branchRestriction = EstablishmentBranchRestriction.Create(
            branch.Id, restriction.Id, RestrictionComplianceLevel.Partial, false, null);
        context.Set<EstablishmentBranchRestriction>().Add(branchRestriction);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repository = new EstablishmentBranchRestrictionRepository(context);
        var found = await repository.GetByIdsAsync(branch.Id, restriction.Id,
            TestContext.Current.CancellationToken);

        Assert.NotNull(found);
        Assert.Equal(branch.Id, found.BranchId);
        Assert.Equal(restriction.Id, found.RestrictionId);
    }

    [Fact]
    public async Task GetByIdsAsync_NonExistent_ReturnsNull()
    {
        using var context = _fixture.CreateContext();
        var repository = new EstablishmentBranchRestrictionRepository(context);

        var found = await repository.GetByIdsAsync(
            EstablishmentBranchId.New(), RestrictionId.New(),
            TestContext.Current.CancellationToken);

        Assert.Null(found);
    }

    [Fact]
    public async Task ExistsAsync_Existing_ReturnsTrue()
    {
        using var context = _fixture.CreateContext();
        var (branch, restriction) = await SeedDependencies(context);

        var branchRestriction = EstablishmentBranchRestriction.Create(
            branch.Id, restriction.Id, RestrictionComplianceLevel.OnRequest, false, null);
        context.Set<EstablishmentBranchRestriction>().Add(branchRestriction);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repository = new EstablishmentBranchRestrictionRepository(context);
        bool exists = await repository.ExistsAsync(branch.Id, restriction.Id,
            TestContext.Current.CancellationToken);

        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsAsync_NonExistent_ReturnsFalse()
    {
        using var context = _fixture.CreateContext();
        var repository = new EstablishmentBranchRestrictionRepository(context);

        bool exists = await repository.ExistsAsync(
            EstablishmentBranchId.New(), RestrictionId.New(),
            TestContext.Current.CancellationToken);

        Assert.False(exists);
    }

    [Fact]
    public async Task DuplicateKey_ThrowsOnAdd()
    {
        using var context = _fixture.CreateContext();
        var (branch, restriction) = await SeedDependencies(context);

        var first = EstablishmentBranchRestriction.Create(
            branch.Id, restriction.Id, RestrictionComplianceLevel.Guaranteed, false, null);
        context.Set<EstablishmentBranchRestriction>().Add(first);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var duplicate = EstablishmentBranchRestriction.Create(
            branch.Id, restriction.Id, RestrictionComplianceLevel.Partial, true, null);

        Assert.Throws<InvalidOperationException>(
            () => context.Set<EstablishmentBranchRestriction>().Add(duplicate));
    }

    [Fact]
    public async Task Audit_CreatedAtUtc_IsSet()
    {
        using var context = _fixture.CreateContext();
        var (branch, restriction) = await SeedDependencies(context);

        var branchRestriction = EstablishmentBranchRestriction.Create(
            branch.Id, restriction.Id, RestrictionComplianceLevel.Guaranteed, false, null);
        context.Set<EstablishmentBranchRestriction>().Add(branchRestriction);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        using var readContext = _fixture.CreateContext();
        var persisted = await readContext.Set<EstablishmentBranchRestriction>()
            .FirstAsync(x => x.BranchId == branch.Id && x.RestrictionId == restriction.Id,
                TestContext.Current.CancellationToken);

        Assert.NotEqual(default, persisted.CreatedAtUtc);
        Assert.Null(persisted.UpdatedAtUtc);
    }

    [Fact]
    public async Task Audit_UpdatedAtUtc_IsSetOnModification()
    {
        using var context = _fixture.CreateContext();
        var (branch, restriction) = await SeedDependencies(context);

        var branchRestriction = EstablishmentBranchRestriction.Create(
            branch.Id, restriction.Id, RestrictionComplianceLevel.Guaranteed, false, null);
        context.Set<EstablishmentBranchRestriction>().Add(branchRestriction);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        branchRestriction.Update(RestrictionComplianceLevel.Partial, true, "Modificado");
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        using var readContext = _fixture.CreateContext();
        var persisted = await readContext.Set<EstablishmentBranchRestriction>()
            .FirstAsync(x => x.BranchId == branch.Id && x.RestrictionId == restriction.Id,
                TestContext.Current.CancellationToken);

        Assert.NotNull(persisted.UpdatedAtUtc);
    }

    [Fact]
    public async Task PersistsAllComplianceLevels()
    {
        using var context = _fixture.CreateContext();
        var (branch, _) = await SeedDependencies(context);

        var r1 = Restriction.Create(RestrictionId.New(), "Sin TACC", null);
        var r2 = Restriction.Create(RestrictionId.New(), "Vegetariano", null);
        var r3 = Restriction.Create(RestrictionId.New(), "Sin lactosa", null);
        context.Set<Restriction>().AddRange(r1, r2, r3);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        context.Set<EstablishmentBranchRestriction>().Add(
            EstablishmentBranchRestriction.Create(branch.Id, r1.Id, RestrictionComplianceLevel.Guaranteed, false, null));
        context.Set<EstablishmentBranchRestriction>().Add(
            EstablishmentBranchRestriction.Create(branch.Id, r2.Id, RestrictionComplianceLevel.Partial, false, null));
        context.Set<EstablishmentBranchRestriction>().Add(
            EstablishmentBranchRestriction.Create(branch.Id, r3.Id, RestrictionComplianceLevel.OnRequest, false, null));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        using var readContext = _fixture.CreateContext();
        var items = await readContext.Set<EstablishmentBranchRestriction>()
            .Where(x => x.BranchId == branch.Id)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Contains(items, x => x.ComplianceLevel == RestrictionComplianceLevel.Guaranteed);
        Assert.Contains(items, x => x.ComplianceLevel == RestrictionComplianceLevel.Partial);
        Assert.Contains(items, x => x.ComplianceLevel == RestrictionComplianceLevel.OnRequest);
    }

    [Fact]
    public async Task PersistsIsCertified()
    {
        using var context = _fixture.CreateContext();
        var (branch, restriction) = await SeedDependencies(context);

        var branchRestriction = EstablishmentBranchRestriction.Create(
            branch.Id, restriction.Id, RestrictionComplianceLevel.Guaranteed, true, null);
        context.Set<EstablishmentBranchRestriction>().Add(branchRestriction);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        using var readContext = _fixture.CreateContext();
        var persisted = await readContext.Set<EstablishmentBranchRestriction>()
            .FirstAsync(x => x.BranchId == branch.Id && x.RestrictionId == restriction.Id,
                TestContext.Current.CancellationToken);

        Assert.True(persisted.IsCertified);
    }

    [Fact]
    public async Task PersistsObservation()
    {
        using var context = _fixture.CreateContext();
        var (branch, restriction) = await SeedDependencies(context);

        var branchRestriction = EstablishmentBranchRestriction.Create(
            branch.Id, restriction.Id, RestrictionComplianceLevel.Partial, false,
            "La cocina utiliza utensilios separados.");
        context.Set<EstablishmentBranchRestriction>().Add(branchRestriction);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        using var readContext = _fixture.CreateContext();
        var persisted = await readContext.Set<EstablishmentBranchRestriction>()
            .FirstAsync(x => x.BranchId == branch.Id && x.RestrictionId == restriction.Id,
                TestContext.Current.CancellationToken);

        Assert.Equal("La cocina utiliza utensilios separados.", persisted.Observation);
    }

    public void Dispose() => _fixture.Dispose();
}
