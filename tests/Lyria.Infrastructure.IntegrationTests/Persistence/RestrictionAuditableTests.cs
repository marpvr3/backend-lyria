using Lyria.Domain.Restrictions;
using Lyria.Infrastructure.Persistence;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Lyria.Infrastructure.IntegrationTests.Persistence;

public sealed class RestrictionAuditableTests : IDisposable
{
    private readonly SqliteFixture _fixture = new();

    [Fact]
    public async Task Insert_SetsCreatedAtUtc()
    {
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2026, 7, 18, 12, 0, 0, TimeSpan.Zero));
        using var context = new LyriaDbContext(_fixture.Options, fakeTime);

        var restriction = Restriction.Create(RestrictionId.New(), "AuditTest" + Guid.NewGuid(), null);
        context.Set<Restriction>().Add(restriction);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.Equal(new DateTime(2026, 7, 18, 12, 0, 0), restriction.CreatedAtUtc);
        Assert.Null(restriction.UpdatedAtUtc);
    }

    [Fact]
    public async Task Update_SetsUpdatedAtUtc_AndPreservesCreatedAtUtc()
    {
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2026, 7, 18, 12, 0, 0, TimeSpan.Zero));
        using var context = new LyriaDbContext(_fixture.Options, fakeTime);

        var restriction = Restriction.Create(RestrictionId.New(), "AuditUpdate" + Guid.NewGuid(), null);
        context.Set<Restriction>().Add(restriction);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var createdAt = restriction.CreatedAtUtc;

        fakeTime.SetUtcNow(new DateTimeOffset(2026, 7, 19, 10, 0, 0, TimeSpan.Zero));
        restriction.Update("Updated" + Guid.NewGuid(), "Descripción actualizada.");
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.Equal(createdAt, restriction.CreatedAtUtc);
        Assert.Equal(new DateTime(2026, 7, 19, 10, 0, 0), restriction.UpdatedAtUtc);
    }

    public void Dispose() => _fixture.Dispose();
}
