using Lyria.Domain.Services;
using Lyria.Infrastructure.Persistence;
using Lyria.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Lyria.Infrastructure.IntegrationTests.Persistence;

public sealed class ServiceRepositoryTests : IDisposable
{
    private readonly SqliteFixture _fixture = new();

    [Fact]
    public async Task AddAsync_And_SaveChanges_PersistsService()
    {
        using var context = _fixture.CreateContext();
        var repository = new ServiceRepository(context);

        var service = Service.Create(ServiceId.New(), "Delivery", "Entrega a domicilio.", "https://cdn.lyria.com/icons/delivery.svg");

        await repository.AddAsync(service, TestContext.Current.CancellationToken);
        await repository.SaveChangesAsync(TestContext.Current.CancellationToken);

        using var readContext = _fixture.CreateContext();
        var persisted = await readContext.Set<Service>().FindAsync([service.Id], TestContext.Current.CancellationToken);

        Assert.NotNull(persisted);
        Assert.Equal("Delivery", persisted.Name);
        Assert.Equal("Entrega a domicilio.", persisted.Description);
        Assert.Equal("https://cdn.lyria.com/icons/delivery.svg", persisted.IconUrl);
        Assert.True(persisted.IsActive);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsService()
    {
        using var context = _fixture.CreateContext();
        var repository = new ServiceRepository(context);

        var service = Service.Create(ServiceId.New(), "Wi-Fi", null, null);
        await repository.AddAsync(service, TestContext.Current.CancellationToken);
        await repository.SaveChangesAsync(TestContext.Current.CancellationToken);

        var found = await repository.GetByIdAsync(service.Id, TestContext.Current.CancellationToken);

        Assert.NotNull(found);
        Assert.Equal(service.Id, found.Id);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentId_ReturnsNull()
    {
        using var context = _fixture.CreateContext();
        var repository = new ServiceRepository(context);

        var found = await repository.GetByIdAsync(ServiceId.New(), TestContext.Current.CancellationToken);

        Assert.Null(found);
    }

    [Fact]
    public async Task ExistsByNameAsync_ExistingName_ReturnsTrue()
    {
        using var context = _fixture.CreateContext();
        var repository = new ServiceRepository(context);

        var service = Service.Create(ServiceId.New(), "Delivery", null, null);
        await repository.AddAsync(service, TestContext.Current.CancellationToken);
        await repository.SaveChangesAsync(TestContext.Current.CancellationToken);

        bool exists = await repository.ExistsByNameAsync("Delivery", excludingId: null, TestContext.Current.CancellationToken);

        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsByNameAsync_ExcludingSameId_ReturnsFalse()
    {
        using var context = _fixture.CreateContext();
        var repository = new ServiceRepository(context);

        var service = Service.Create(ServiceId.New(), "Takeaway", null, null);
        await repository.AddAsync(service, TestContext.Current.CancellationToken);
        await repository.SaveChangesAsync(TestContext.Current.CancellationToken);

        bool exists = await repository.ExistsByNameAsync("Takeaway", excludingId: service.Id, TestContext.Current.CancellationToken);

        Assert.False(exists);
    }

    [Fact]
    public async Task ExistsByNameAsync_NonExistentName_ReturnsFalse()
    {
        using var context = _fixture.CreateContext();
        var repository = new ServiceRepository(context);

        bool exists = await repository.ExistsByNameAsync("No existe", excludingId: null, TestContext.Current.CancellationToken);

        Assert.False(exists);
    }

    [Fact]
    public async Task ExistsByNameAsync_DifferentCase_ReturnsTrue()
    {
        using var context = _fixture.CreateContext();
        var repository = new ServiceRepository(context);

        var service = Service.Create(ServiceId.New(), "Delivery", null, null);
        await repository.AddAsync(service, TestContext.Current.CancellationToken);
        await repository.SaveChangesAsync(TestContext.Current.CancellationToken);

        bool exists = await repository.ExistsByNameAsync("delivery", excludingId: null, TestContext.Current.CancellationToken);

        Assert.True(exists);
    }

    [Fact]
    public async Task AddAsync_PersistsIconUrl()
    {
        using var context = _fixture.CreateContext();
        var repository = new ServiceRepository(context);

        var service = Service.Create(ServiceId.New(), "Reservas", null, "https://cdn.lyria.com/icons/reservas.svg");
        await repository.AddAsync(service, TestContext.Current.CancellationToken);
        await repository.SaveChangesAsync(TestContext.Current.CancellationToken);

        using var readContext = _fixture.CreateContext();
        var persisted = await readContext.Set<Service>().FindAsync([service.Id], TestContext.Current.CancellationToken);

        Assert.NotNull(persisted);
        Assert.Equal("https://cdn.lyria.com/icons/reservas.svg", persisted.IconUrl);
    }

    public void Dispose() => _fixture.Dispose();
}
