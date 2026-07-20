using Lyria.Domain.Services;
using Lyria.Infrastructure.Persistence;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Lyria.Infrastructure.IntegrationTests.Persistence;

public sealed class ServiceAuditableTests : IDisposable
{
    private readonly SqliteFixture _fixture = new();

    [Fact]
    public async Task Insert_SetsCreatedAtUtc()
    {
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2026, 7, 18, 12, 0, 0, TimeSpan.Zero));
        using var context = new LyriaDbContext(_fixture.Options, fakeTime);

        var service = Service.Create(ServiceId.New(), "AuditTest" + Guid.NewGuid(), null, null);
        context.Set<Service>().Add(service);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.Equal(new DateTime(2026, 7, 18, 12, 0, 0), service.CreatedAtUtc);
        Assert.Null(service.UpdatedAtUtc);
    }

    [Fact]
    public async Task Update_SetsUpdatedAtUtc_AndPreservesCreatedAtUtc()
    {
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2026, 7, 18, 12, 0, 0, TimeSpan.Zero));
        using var context = new LyriaDbContext(_fixture.Options, fakeTime);

        var service = Service.Create(ServiceId.New(), "AuditUpdate" + Guid.NewGuid(), null, null);
        context.Set<Service>().Add(service);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var createdAt = service.CreatedAtUtc;

        fakeTime.SetUtcNow(new DateTimeOffset(2026, 7, 19, 10, 0, 0, TimeSpan.Zero));
        service.Update("Updated" + Guid.NewGuid(), "Descripción actualizada.", null);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.Equal(createdAt, service.CreatedAtUtc);
        Assert.Equal(new DateTime(2026, 7, 19, 10, 0, 0), service.UpdatedAtUtc);
    }

    public void Dispose() => _fixture.Dispose();
}
