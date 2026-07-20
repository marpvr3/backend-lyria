using Lyria.Domain.Establishments;
using Lyria.Domain.Establishments.Categories;
using Lyria.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Lyria.Infrastructure.IntegrationTests.Persistence;

public sealed class AuditableEntityTests : IDisposable
{
    private static readonly DateTimeOffset FixedTime =
        new(2026, 7, 15, 10, 30, 0, TimeSpan.Zero);

    private readonly FakeTimeProvider _timeProvider;
    private readonly SqliteFixture _fixture;

    public AuditableEntityTests()
    {
        _timeProvider = new FakeTimeProvider(FixedTime);
        _fixture = new SqliteFixture(_timeProvider);
    }

    // --- EstablishmentCategory ---

    [Fact]
    public async Task NewCategory_Should_Have_CreatedAtUtc()
    {
        var category = EstablishmentCategory.Create(
            EstablishmentCategoryId.New(), "Test", null, null, 1);

        await using var context = _fixture.CreateContext();
        context.Set<EstablishmentCategory>().Add(category);
        await context.SaveChangesAsync(CancellationToken.None);

        Assert.Equal(FixedTime.UtcDateTime, category.CreatedAtUtc);
    }

    [Fact]
    public async Task NewCategory_Should_Have_Null_UpdatedAtUtc()
    {
        var category = EstablishmentCategory.Create(
            EstablishmentCategoryId.New(), "Test", null, null, 1);

        await using var context = _fixture.CreateContext();
        context.Set<EstablishmentCategory>().Add(category);
        await context.SaveChangesAsync(CancellationToken.None);

        Assert.Null(category.UpdatedAtUtc);
    }

    [Fact]
    public async Task ModifiedCategory_Should_Set_UpdatedAtUtc()
    {
        var category = EstablishmentCategory.Create(
            EstablishmentCategoryId.New(), "Test", null, null, 1);

        await using (var context = _fixture.CreateContext())
        {
            context.Set<EstablishmentCategory>().Add(category);
            await context.SaveChangesAsync(CancellationToken.None);
        }

        var updateTime = FixedTime.AddHours(1);
        _timeProvider.SetUtcNow(updateTime);

        await using (var context = _fixture.CreateContext())
        {
            var tracked = await context.Set<EstablishmentCategory>()
                .FirstAsync(c => c.Id == category.Id, CancellationToken.None);
            tracked.UpdateDetails("Updated", null, null, 2);
            await context.SaveChangesAsync(CancellationToken.None);

            Assert.Equal(updateTime.UtcDateTime, tracked.UpdatedAtUtc);
        }
    }

    [Fact]
    public async Task ModifiedCategory_Should_Not_Change_CreatedAtUtc()
    {
        var category = EstablishmentCategory.Create(
            EstablishmentCategoryId.New(), "Test", null, null, 1);

        await using (var context = _fixture.CreateContext())
        {
            context.Set<EstablishmentCategory>().Add(category);
            await context.SaveChangesAsync(CancellationToken.None);
        }

        DateTime originalCreatedAt = category.CreatedAtUtc;

        _timeProvider.SetUtcNow(FixedTime.AddHours(1));

        await using (var context = _fixture.CreateContext())
        {
            var tracked = await context.Set<EstablishmentCategory>()
                .FirstAsync(c => c.Id == category.Id, CancellationToken.None);
            tracked.UpdateDetails("Updated", null, null, 2);
            await context.SaveChangesAsync(CancellationToken.None);

            Assert.Equal(originalCreatedAt, tracked.CreatedAtUtc);
        }
    }

    // --- Establishment ---

    [Fact]
    public async Task NewEstablishment_Should_Have_CreatedAtUtc()
    {
        var categoryId = await SeedCategoryAsync();

        var establishment = Establishment.Create(
            EstablishmentId.New(), categoryId, "Test", "test", null, null, null, null, null, null);

        await using var context = _fixture.CreateContext();
        context.Set<Establishment>().Add(establishment);
        await context.SaveChangesAsync(CancellationToken.None);

        Assert.Equal(FixedTime.UtcDateTime, establishment.CreatedAtUtc);
    }

    [Fact]
    public async Task NewEstablishment_Should_Have_Null_UpdatedAtUtc()
    {
        var categoryId = await SeedCategoryAsync();

        var establishment = Establishment.Create(
            EstablishmentId.New(), categoryId, "Test", "test", null, null, null, null, null, null);

        await using var context = _fixture.CreateContext();
        context.Set<Establishment>().Add(establishment);
        await context.SaveChangesAsync(CancellationToken.None);

        Assert.Null(establishment.UpdatedAtUtc);
    }

    [Fact]
    public async Task ModifiedEstablishment_Should_Set_UpdatedAtUtc()
    {
        var categoryId = await SeedCategoryAsync();
        var establishment = Establishment.Create(
            EstablishmentId.New(), categoryId, "Test", "test", null, null, null, null, null, null);

        await using (var context = _fixture.CreateContext())
        {
            context.Set<Establishment>().Add(establishment);
            await context.SaveChangesAsync(CancellationToken.None);
        }

        var updateTime = FixedTime.AddHours(2);
        _timeProvider.SetUtcNow(updateTime);

        await using (var context = _fixture.CreateContext())
        {
            var tracked = await context.Set<Establishment>()
                .FirstAsync(e => e.Id == establishment.Id, CancellationToken.None);
            tracked.UpdateDetails("Updated", "test", null, null, null, null, null, null);
            await context.SaveChangesAsync(CancellationToken.None);

            Assert.Equal(updateTime.UtcDateTime, tracked.UpdatedAtUtc);
        }
    }

    [Fact]
    public async Task ModifiedEstablishment_Should_Not_Change_CreatedAtUtc()
    {
        var categoryId = await SeedCategoryAsync();
        var establishment = Establishment.Create(
            EstablishmentId.New(), categoryId, "Test", "test", null, null, null, null, null, null);

        await using (var context = _fixture.CreateContext())
        {
            context.Set<Establishment>().Add(establishment);
            await context.SaveChangesAsync(CancellationToken.None);
        }

        DateTime originalCreatedAt = establishment.CreatedAtUtc;

        _timeProvider.SetUtcNow(FixedTime.AddHours(2));

        await using (var context = _fixture.CreateContext())
        {
            var tracked = await context.Set<Establishment>()
                .FirstAsync(e => e.Id == establishment.Id, CancellationToken.None);
            tracked.UpdateDetails("Updated", "test", null, null, null, null, null, null);
            await context.SaveChangesAsync(CancellationToken.None);

            Assert.Equal(originalCreatedAt, tracked.CreatedAtUtc);
        }
    }

    // --- UTC validation ---

    [Fact]
    public async Task CreatedAtUtc_Should_Be_Stored_In_Utc()
    {
        var category = EstablishmentCategory.Create(
            EstablishmentCategoryId.New(), "Utc Test", null, null, 1);

        await using (var context = _fixture.CreateContext())
        {
            context.Set<EstablishmentCategory>().Add(category);
            await context.SaveChangesAsync(CancellationToken.None);
        }

        await using (var readContext = _fixture.CreateContext())
        {
            var loaded = await readContext.Set<EstablishmentCategory>()
                .AsNoTracking()
                .FirstAsync(c => c.Id == category.Id, CancellationToken.None);

            Assert.Equal(FixedTime.UtcDateTime, loaded.CreatedAtUtc);
        }
    }

    // --- Column name tests ---

    [Fact]
    public void EstablishmentCategory_CreatedAtUtc_Column_Should_Be_FechaCreacion()
    {
        using var context = _fixture.CreateContext();
        var entityType = context.Model.FindEntityType(typeof(EstablishmentCategory))!;
        var property = entityType.FindProperty(nameof(EstablishmentCategory.CreatedAtUtc))!;

        Assert.Equal("FechaCreacion", property.GetColumnName());
    }

    [Fact]
    public void EstablishmentCategory_UpdatedAtUtc_Column_Should_Be_FechaActualizacion()
    {
        using var context = _fixture.CreateContext();
        var entityType = context.Model.FindEntityType(typeof(EstablishmentCategory))!;
        var property = entityType.FindProperty(nameof(EstablishmentCategory.UpdatedAtUtc))!;

        Assert.Equal("FechaActualizacion", property.GetColumnName());
    }

    [Fact]
    public void Establishment_CreatedAtUtc_Column_Should_Be_FechaCreacion()
    {
        using var context = _fixture.CreateContext();
        var entityType = context.Model.FindEntityType(typeof(Establishment))!;
        var property = entityType.FindProperty(nameof(Establishment.CreatedAtUtc))!;

        Assert.Equal("FechaCreacion", property.GetColumnName());
    }

    [Fact]
    public void Establishment_UpdatedAtUtc_Column_Should_Be_FechaActualizacion()
    {
        using var context = _fixture.CreateContext();
        var entityType = context.Model.FindEntityType(typeof(Establishment))!;
        var property = entityType.FindProperty(nameof(Establishment.UpdatedAtUtc))!;

        Assert.Equal("FechaActualizacion", property.GetColumnName());
    }

    // --- Synchronous SaveChanges ---

    [Fact]
    public void SaveChanges_Should_Set_CreatedAtUtc()
    {
        var category = EstablishmentCategory.Create(
            EstablishmentCategoryId.New(), "Sync Test", null, null, 1);

        using var context = _fixture.CreateContext();
        context.Set<EstablishmentCategory>().Add(category);
        context.SaveChanges();

        Assert.Equal(FixedTime.UtcDateTime, category.CreatedAtUtc);
    }

    [Fact]
    public void SaveChanges_Modified_Should_Set_UpdatedAtUtc()
    {
        var category = EstablishmentCategory.Create(
            EstablishmentCategoryId.New(), "Sync Test", null, null, 1);

        using (var context = _fixture.CreateContext())
        {
            context.Set<EstablishmentCategory>().Add(category);
            context.SaveChanges();
        }

        var updateTime = FixedTime.AddHours(3);
        _timeProvider.SetUtcNow(updateTime);

        using (var context = _fixture.CreateContext())
        {
            var tracked = context.Set<EstablishmentCategory>()
                .First(c => c.Id == category.Id);
            tracked.UpdateDetails("Updated Sync", null, null, 2);
            context.SaveChanges();

            Assert.Equal(updateTime.UtcDateTime, tracked.UpdatedAtUtc);
        }
    }

    [Fact]
    public void SaveChanges_Modified_Should_Not_Change_CreatedAtUtc()
    {
        var category = EstablishmentCategory.Create(
            EstablishmentCategoryId.New(), "Sync Test", null, null, 1);

        using (var context = _fixture.CreateContext())
        {
            context.Set<EstablishmentCategory>().Add(category);
            context.SaveChanges();
        }

        DateTime originalCreatedAt = category.CreatedAtUtc;

        _timeProvider.SetUtcNow(FixedTime.AddHours(3));

        using (var context = _fixture.CreateContext())
        {
            var tracked = context.Set<EstablishmentCategory>()
                .First(c => c.Id == category.Id);
            tracked.UpdateDetails("Updated Sync", null, null, 2);
            context.SaveChanges();

            Assert.Equal(originalCreatedAt, tracked.CreatedAtUtc);
        }
    }

    public void Dispose()
    {
        _fixture.Dispose();
    }

    private async Task<EstablishmentCategoryId> SeedCategoryAsync()
    {
        var categoryId = EstablishmentCategoryId.New();
        await using var context = _fixture.CreateContext();
        var category = EstablishmentCategory.Create(categoryId, "Restaurante", null, null, 1);
        context.Set<EstablishmentCategory>().Add(category);
        await context.SaveChangesAsync(CancellationToken.None);
        return categoryId;
    }
}
