using Lyria.Domain.Services;
using Xunit;

namespace Lyria.Domain.UnitTests.Services;

public sealed class ServiceStateTests
{
    private static Service CreateService() =>
        Service.Create(ServiceId.New(), "Delivery", null, null);

    [Fact]
    public void Update_ChangesNameDescriptionAndIconUrl()
    {
        var service = CreateService();
        service.Update("Wi-Fi", "Conexión inalámbrica.", "https://cdn.lyria.com/icons/wifi.svg");

        Assert.Equal("Wi-Fi", service.Name);
        Assert.Equal("Conexión inalámbrica.", service.Description);
        Assert.Equal("https://cdn.lyria.com/icons/wifi.svg", service.IconUrl);
    }

    [Fact]
    public void Update_TrimsName()
    {
        var service = CreateService();
        service.Update("  Wi-Fi  ", null, null);
        Assert.Equal("Wi-Fi", service.Name);
    }

    [Fact]
    public void Update_EmptyDescription_NormalizesToNull()
    {
        var service = Service.Create(ServiceId.New(), "Delivery", "Una descripción.", null);
        service.Update("Delivery", "", null);
        Assert.Null(service.Description);
    }

    [Fact]
    public void Update_InvalidName_Throws()
    {
        var service = CreateService();
        Assert.Throws<ServiceException>(() =>
            service.Update("", null, null));
    }

    [Fact]
    public void Update_DoesNotChangeIsActive()
    {
        var service = CreateService();
        service.Deactivate();
        service.Update("Wi-Fi", null, null);
        Assert.False(service.IsActive);
    }

    [Fact]
    public void Activate_SetsIsActiveTrue()
    {
        var service = CreateService();
        service.Deactivate();
        service.Activate();
        Assert.True(service.IsActive);
    }

    [Fact]
    public void Deactivate_SetsIsActiveFalse()
    {
        var service = CreateService();
        service.Deactivate();
        Assert.False(service.IsActive);
    }

    [Fact]
    public void Activate_WhenAlreadyActive_IsIdempotent()
    {
        var service = CreateService();
        Assert.True(service.IsActive);
        service.Activate();
        Assert.True(service.IsActive);
    }

    [Fact]
    public void Deactivate_WhenAlreadyInactive_IsIdempotent()
    {
        var service = CreateService();
        service.Deactivate();
        Assert.False(service.IsActive);
        service.Deactivate();
        Assert.False(service.IsActive);
    }

    [Fact]
    public void Update_PreservesId()
    {
        var id = ServiceId.New();
        var service = Service.Create(id, "Delivery", null, null);
        service.Update("Wi-Fi", "Descripción.", "https://cdn.lyria.com/icons/wifi.svg");
        Assert.Equal(id, service.Id);
    }
}
