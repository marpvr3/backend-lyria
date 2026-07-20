using Lyria.Domain.Establishments;
using Lyria.Domain.Establishments.Branches;
using Lyria.Domain.Establishments.Categories;
using Lyria.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Lyria.Infrastructure.IntegrationTests.Persistence;

public sealed class EstablishmentBranchRepositoryTests : IDisposable
{
    private readonly SqliteFixture _fixture = new();

    private async Task<(EstablishmentCategoryId CategoryId, EstablishmentId EstablishmentId)> SeedEstablishmentAsync()
    {
        var categoryId = EstablishmentCategoryId.New();
        var establishmentId = EstablishmentId.New();

        await using var context = _fixture.CreateContext();

        var category = EstablishmentCategory.Create(
            categoryId, "Restaurante", null, null, 1);
        context.Set<EstablishmentCategory>().Add(category);

        var establishment = Establishment.Create(
            establishmentId, categoryId, "Let It V", "let-it-v",
            "Vegano", "https://letitv.com", "@letitv", null, null, null);
        context.Set<Establishment>().Add(establishment);

        await context.SaveChangesAsync(CancellationToken.None);

        return (categoryId, establishmentId);
    }

    [Fact]
    public async Task Add_And_SaveChangesAsync_Should_Persist_Branch()
    {
        var (_, establishmentId) = await SeedEstablishmentAsync();

        var branch = EstablishmentBranch.Create(
            EstablishmentBranchId.New(), establishmentId, "Palermo", "Costa Rica",
            "5865", null, "Palermo", "Buenos Aires", "Buenos Aires", "C1414", "Argentina",
            -34.586m, -58.432m, null, null, null);

        await using (var context = _fixture.CreateContext())
        {
            var repository = new EstablishmentBranchRepository(context);
            repository.Add(branch);
            await repository.SaveChangesAsync(CancellationToken.None);
        }

        await using (var context = _fixture.CreateContext())
        {
            var persisted = await context.Set<EstablishmentBranch>()
                .FirstOrDefaultAsync(e => e.Id == branch.Id, CancellationToken.None);

            Assert.NotNull(persisted);
            Assert.Equal("Palermo", persisted.Name);
            Assert.Equal("Costa Rica", persisted.Street);
            Assert.Equal("5865", persisted.Number);
            Assert.Equal("Palermo", persisted.Neighborhood);
            Assert.Equal("Buenos Aires", persisted.City);
            Assert.Equal("Argentina", persisted.Country);
            Assert.True(persisted.IsActive);
        }
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Persisted_Branch()
    {
        var (_, establishmentId) = await SeedEstablishmentAsync();

        var branch = EstablishmentBranch.Create(
            EstablishmentBranchId.New(), establishmentId, "Centro", "Av. Corrientes",
            "1234", null, "Centro", "Buenos Aires", "Buenos Aires", "C1000", "Argentina",
            -34.604m, -58.381m, null, null, null);

        await using (var context = _fixture.CreateContext())
        {
            var repository = new EstablishmentBranchRepository(context);
            repository.Add(branch);
            await repository.SaveChangesAsync(CancellationToken.None);
        }

        await using (var context = _fixture.CreateContext())
        {
            var repository = new EstablishmentBranchRepository(context);
            var result = await repository.GetByIdAsync(branch.Id, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(branch.Id, result.Id);
            Assert.Equal("Centro", result.Name);
        }
    }

    [Fact]
    public async Task ExistsByNameWithinEstablishmentAsync_Should_Return_True_For_Existing_Name()
    {
        var (_, establishmentId) = await SeedEstablishmentAsync();

        var branch = EstablishmentBranch.Create(
            EstablishmentBranchId.New(), establishmentId, "Palermo", "Costa Rica",
            "5865", null, "Palermo", "Buenos Aires", "Buenos Aires", "C1414", "Argentina",
            -34.586m, -58.432m, null, null, null);

        await using (var context = _fixture.CreateContext())
        {
            var repository = new EstablishmentBranchRepository(context);
            repository.Add(branch);
            await repository.SaveChangesAsync(CancellationToken.None);
        }

        await using (var context = _fixture.CreateContext())
        {
            var repository = new EstablishmentBranchRepository(context);
            var exists = await repository.ExistsByNameWithinEstablishmentAsync(
                establishmentId, "Palermo", null, CancellationToken.None);

            Assert.True(exists);
        }
    }

    [Fact]
    public async Task ExistsByNameWithinEstablishmentAsync_Should_Return_False_For_NonExistent_Name()
    {
        var (_, establishmentId) = await SeedEstablishmentAsync();

        await using var context = _fixture.CreateContext();
        var repository = new EstablishmentBranchRepository(context);

        var exists = await repository.ExistsByNameWithinEstablishmentAsync(
            establishmentId, "Inexistente", null, CancellationToken.None);

        Assert.False(exists);
    }

    [Fact]
    public async Task ExistsByIdAsync_Should_Return_True_For_Existing_Id()
    {
        var (_, establishmentId) = await SeedEstablishmentAsync();

        var branch = EstablishmentBranch.Create(
            EstablishmentBranchId.New(), establishmentId, "Palermo", "Costa Rica",
            "5865", null, "Palermo", "Buenos Aires", "Buenos Aires", "C1414", "Argentina",
            -34.586m, -58.432m, null, null, null);

        await using (var context = _fixture.CreateContext())
        {
            var repository = new EstablishmentBranchRepository(context);
            repository.Add(branch);
            await repository.SaveChangesAsync(CancellationToken.None);
        }

        await using (var context = _fixture.CreateContext())
        {
            var repository = new EstablishmentBranchRepository(context);
            var exists = await repository.ExistsByIdAsync(branch.Id, CancellationToken.None);

            Assert.True(exists);
        }
    }

    [Fact]
    public async Task ExistsByIdAsync_Should_Return_False_For_NonExistent_Id()
    {
        await using var context = _fixture.CreateContext();
        var repository = new EstablishmentBranchRepository(context);

        var exists = await repository.ExistsByIdAsync(
            EstablishmentBranchId.New(), CancellationToken.None);

        Assert.False(exists);
    }

    public void Dispose()
    {
        _fixture.Dispose();
    }
}
