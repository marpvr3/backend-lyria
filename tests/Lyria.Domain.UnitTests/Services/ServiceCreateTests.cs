using Lyria.Domain.Services;
using Xunit;

namespace Lyria.Domain.UnitTests.Services;

public sealed class ServiceCreateTests
{
    [Fact]
    public void Create_ValidData_SetsAllProperties()
    {
        var id = ServiceId.New();
        var service = Service.Create(id, "Delivery", "Entrega a domicilio.", "https://cdn.lyria.com/icons/delivery.svg");

        Assert.Equal(id, service.Id);
        Assert.Equal("Delivery", service.Name);
        Assert.Equal("Entrega a domicilio.", service.Description);
        Assert.Equal("https://cdn.lyria.com/icons/delivery.svg", service.IconUrl);
    }

    [Fact]
    public void Create_NewService_StartsActive()
    {
        var service = Service.Create(ServiceId.New(), "Delivery", null, null);
        Assert.True(service.IsActive);
    }

    [Fact]
    public void Create_NameWithSpaces_AppliesTrim()
    {
        var service = Service.Create(ServiceId.New(), "  Delivery  ", null, null);
        Assert.Equal("Delivery", service.Name);
    }

    [Fact]
    public void Create_NameWithMultipleSpaces_CollapsesSpaces()
    {
        var service = Service.Create(ServiceId.New(), "Comer   en   el   lugar", null, null);
        Assert.Equal("Comer en el lugar", service.Name);
    }

    [Fact]
    public void Create_NullName_Throws()
    {
        Assert.Throws<ServiceException>(() =>
            Service.Create(ServiceId.New(), null!, null, null));
    }

    [Fact]
    public void Create_EmptyName_Throws()
    {
        Assert.Throws<ServiceException>(() =>
            Service.Create(ServiceId.New(), "", null, null));
    }

    [Fact]
    public void Create_NameTooShort_Throws()
    {
        Assert.Throws<ServiceException>(() =>
            Service.Create(ServiceId.New(), "A", null, null));
    }

    [Fact]
    public void Create_NameTooLong_Throws()
    {
        string longName = new('A', Service.NameMaxLength + 1);
        Assert.Throws<ServiceException>(() =>
            Service.Create(ServiceId.New(), longName, null, null));
    }

    [Fact]
    public void Create_EmptyDescription_NormalizesToNull()
    {
        var service = Service.Create(ServiceId.New(), "Delivery", "", null);
        Assert.Null(service.Description);
    }

    [Fact]
    public void Create_WhitespaceDescription_NormalizesToNull()
    {
        var service = Service.Create(ServiceId.New(), "Delivery", "   ", null);
        Assert.Null(service.Description);
    }

    [Fact]
    public void Create_DescriptionTooLong_Throws()
    {
        string longDesc = new('A', Service.DescriptionMaxLength + 1);
        Assert.Throws<ServiceException>(() =>
            Service.Create(ServiceId.New(), "Delivery", longDesc, null));
    }

    [Fact]
    public void Create_IconUrlNull_IsValid()
    {
        var service = Service.Create(ServiceId.New(), "Delivery", null, null);
        Assert.Null(service.IconUrl);
    }

    [Fact]
    public void Create_IconUrlEmpty_NormalizesToNull()
    {
        var service = Service.Create(ServiceId.New(), "Delivery", null, "");
        Assert.Null(service.IconUrl);
    }

    [Fact]
    public void Create_IconUrlWhitespace_NormalizesToNull()
    {
        var service = Service.Create(ServiceId.New(), "Delivery", null, "   ");
        Assert.Null(service.IconUrl);
    }

    [Fact]
    public void Create_IconUrlHttp_IsValid()
    {
        var service = Service.Create(ServiceId.New(), "Delivery", null, "http://cdn.lyria.com/icons/delivery.svg");
        Assert.Equal("http://cdn.lyria.com/icons/delivery.svg", service.IconUrl);
    }

    [Fact]
    public void Create_IconUrlHttps_IsValid()
    {
        var service = Service.Create(ServiceId.New(), "Delivery", null, "https://cdn.lyria.com/icons/delivery.svg");
        Assert.Equal("https://cdn.lyria.com/icons/delivery.svg", service.IconUrl);
    }

    [Fact]
    public void Create_IconUrlRelative_Throws()
    {
        Assert.Throws<ServiceException>(() =>
            Service.Create(ServiceId.New(), "Delivery", null, "delivery.svg"));
    }

    [Fact]
    public void Create_IconUrlFtp_Throws()
    {
        Assert.Throws<ServiceException>(() =>
            Service.Create(ServiceId.New(), "Delivery", null, "ftp://servidor/icono.svg"));
    }

    [Fact]
    public void Create_IconUrlTooLong_Throws()
    {
        string longUrl = "https://cdn.lyria.com/" + new string('a', Service.IconUrlMaxLength);
        Assert.Throws<ServiceException>(() =>
            Service.Create(ServiceId.New(), "Delivery", null, longUrl));
    }

    [Fact]
    public void NoPublicSetters()
    {
        var properties = typeof(Service).GetProperties();
        foreach (var prop in properties)
        {
            var setter = prop.GetSetMethod(nonPublic: false);
            Assert.Null(setter);
        }
    }
}
