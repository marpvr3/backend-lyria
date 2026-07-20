using Lyria.Domain.Establishments.Categories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Lyria.Infrastructure.IntegrationTests.Persistence;

public sealed class UniqueConstraintTests : IDisposable
{
    private readonly SqliteFixture _fixture = new();

    [Fact]
    public async Task Should_Allow_Persisting_Multiple_Categories_With_Different_Names()
    {
        var first = EstablishmentCategory.Create(
            EstablishmentCategoryId.New(), "Restaurante", null, null, 1);
        var second = EstablishmentCategory.Create(
            EstablishmentCategoryId.New(), "Cafetería", null, null, 2);

        await using var context = _fixture.CreateContext();
        context.Set<EstablishmentCategory>().AddRange(first, second);

        await context.SaveChangesAsync(CancellationToken.None);

        Assert.Equal(2, context.Set<EstablishmentCategory>().Count());
    }

    [Fact]
    public async Task Should_Reject_Duplicate_Name()
    {
        var first = EstablishmentCategory.Create(
            EstablishmentCategoryId.New(), "Restaurante", null, null, 1);

        await using (var context = _fixture.CreateContext())
        {
            context.Set<EstablishmentCategory>().Add(first);
            await context.SaveChangesAsync(CancellationToken.None);
        }

        var duplicate = EstablishmentCategory.Create(
            EstablishmentCategoryId.New(), "Restaurante", null, null, 2);

        await using (var context = _fixture.CreateContext())
        {
            context.Set<EstablishmentCategory>().Add(duplicate);

            await Assert.ThrowsAsync<DbUpdateException>(
                () => context.SaveChangesAsync(CancellationToken.None));
        }
    }

    [Fact]
    public async Task Should_Reject_Duplicate_Name_Different_Casing()
    {
        var first = EstablishmentCategory.Create(
            EstablishmentCategoryId.New(), "Restaurante", null, null, 1);

        await using (var context = _fixture.CreateContext())
        {
            context.Set<EstablishmentCategory>().Add(first);
            await context.SaveChangesAsync(CancellationToken.None);
        }

        var duplicate = EstablishmentCategory.Create(
            EstablishmentCategoryId.New(), "RESTAURANTE", null, null, 2);

        await using (var context = _fixture.CreateContext())
        {
            context.Set<EstablishmentCategory>().Add(duplicate);

            await Assert.ThrowsAsync<DbUpdateException>(
                () => context.SaveChangesAsync(CancellationToken.None));
        }
    }

    [Fact]
    public async Task Relationships_Use_CategoriaEstablecimientoId()
    {
        await using var context = _fixture.CreateContext();

        var entityType = context.Model.FindEntityType(typeof(EstablishmentCategory))!;
        var idProperty = entityType.FindProperty(nameof(EstablishmentCategory.Id))!;

        Assert.Equal("CategoriaEstablecimientoId", idProperty.GetColumnName());
    }

    public void Dispose()
    {
        _fixture.Dispose();
    }
}
