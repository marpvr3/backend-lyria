using Lyria.Api.Extensions;
using Xunit;

namespace Lyria.Api.UnitTests;

public class SensitiveDataRedactorTests
{
    [Fact]
    public void Redact_Password_ReturnsRedacted()
    {
        string json = """{"password":"secret123"}""";

        string? result = SensitiveDataRedactor.Redact(json);

        Assert.Contains("***REDACTED***", result);
        Assert.DoesNotContain("secret123", result);
    }

    [Fact]
    public void Redact_Token_ReturnsRedacted()
    {
        string json = """{"token":"abc.def.ghi"}""";

        string? result = SensitiveDataRedactor.Redact(json);

        Assert.Contains("***REDACTED***", result);
        Assert.DoesNotContain("abc.def.ghi", result);
    }

    [Fact]
    public void Redact_Authorization_ReturnsRedacted()
    {
        string json = """{"authorization":"Bearer xyz"}""";

        string? result = SensitiveDataRedactor.Redact(json);

        Assert.Contains("***REDACTED***", result);
        Assert.DoesNotContain("Bearer xyz", result);
    }

    [Fact]
    public void Redact_NestedProperties_ReturnsRedacted()
    {
        string json = """{"user":{"credentials":{"password":"mypass","name":"John"}}}""";

        string? result = SensitiveDataRedactor.Redact(json);

        Assert.Contains("***REDACTED***", result);
        Assert.DoesNotContain("mypass", result);
        Assert.Contains("John", result);
    }

    [Fact]
    public void Redact_PropertiesInArrays_ReturnsRedacted()
    {
        string json = """{"users":[{"password":"pass1"},{"password":"pass2"}]}""";

        string? result = SensitiveDataRedactor.Redact(json);

        Assert.DoesNotContain("pass1", result);
        Assert.DoesNotContain("pass2", result);
    }

    [Fact]
    public void Redact_IgnoresCasing()
    {
        string json = """{"PASSWORD":"secret","Token":"abc","ApiKey":"key123"}""";

        string? result = SensitiveDataRedactor.Redact(json);

        Assert.DoesNotContain("secret", result);
        Assert.DoesNotContain("abc", result);
        Assert.DoesNotContain("key123", result);
    }

    [Fact]
    public void Redact_PreservesNonSensitiveProperties()
    {
        string json = """{"name":"John","description":"A description","age":30}""";

        string? result = SensitiveDataRedactor.Redact(json);

        Assert.Contains("John", result);
        Assert.Contains("A description", result);
        Assert.Contains("30", result);
        Assert.DoesNotContain("***REDACTED***", result);
    }

    [Fact]
    public void Redact_Email_ReturnsRedacted()
    {
        string json = """{"email":"user@example.com"}""";

        string? result = SensitiveDataRedactor.Redact(json);

        Assert.DoesNotContain("user@example.com", result);
        Assert.Contains("***REDACTED***", result);
    }

    [Fact]
    public void Redact_ContactEmail_ReturnsRedacted()
    {
        string json = """{"contactEmail":"contact@example.com"}""";

        string? result = SensitiveDataRedactor.Redact(json);

        Assert.DoesNotContain("contact@example.com", result);
        Assert.Contains("***REDACTED***", result);
    }

    [Fact]
    public void Redact_Phone_ReturnsRedacted()
    {
        string json = """{"phone":"+573001234567"}""";

        string? result = SensitiveDataRedactor.Redact(json);

        Assert.DoesNotContain("+573001234567", result);
    }

    [Fact]
    public void Redact_ContactPhone_ReturnsRedacted()
    {
        string json = """{"contactPhone":"3009876543"}""";

        string? result = SensitiveDataRedactor.Redact(json);

        Assert.DoesNotContain("3009876543", result);
    }

    [Fact]
    public void Redact_WhatsApp_ReturnsRedacted()
    {
        string json = """{"whatsApp":"+573115551234"}""";

        string? result = SensitiveDataRedactor.Redact(json);

        Assert.DoesNotContain("+573115551234", result);
    }

    [Fact]
    public void Redact_Street_ReturnsRedacted()
    {
        string json = """{"street":"Calle 123 # 45-67"}""";

        string? result = SensitiveDataRedactor.Redact(json);

        Assert.DoesNotContain("Calle 123", result);
    }

    [Fact]
    public void Redact_Number_ReturnsRedacted()
    {
        string json = """{"number":"45-67"}""";

        string? result = SensitiveDataRedactor.Redact(json);

        Assert.DoesNotContain("45-67", result);
    }

    [Fact]
    public void Redact_PostalCode_ReturnsRedacted()
    {
        string json = """{"postalCode":"110111"}""";

        string? result = SensitiveDataRedactor.Redact(json);

        Assert.DoesNotContain("110111", result);
    }

    [Fact]
    public void Redact_LatitudeAndLongitude_NumericValues_ReturnsRedacted()
    {
        string json = """{"latitude":4.6097,"longitude":-74.0817}""";

        string? result = SensitiveDataRedactor.Redact(json);

        Assert.DoesNotContain("4.6097", result);
        Assert.DoesNotContain("-74.0817", result);
    }

    [Fact]
    public void Redact_PersonalData_NestedProperties_ReturnsRedacted()
    {
        string json = """{"branch":{"location":{"street":"Carrera 7","postalCode":"110231","city":"Bogota"}}}""";

        string? result = SensitiveDataRedactor.Redact(json);

        Assert.DoesNotContain("Carrera 7", result);
        Assert.DoesNotContain("110231", result);
        Assert.Contains("Bogota", result);
    }

    [Fact]
    public void Redact_PersonalData_InArrays_ReturnsRedacted()
    {
        string json = """{"branches":[{"email":"a@b.com"},{"email":"c@d.com"}]}""";

        string? result = SensitiveDataRedactor.Redact(json);

        Assert.DoesNotContain("a@b.com", result);
        Assert.DoesNotContain("c@d.com", result);
    }

    [Fact]
    public void Redact_PersonalData_IgnoresCasing()
    {
        string json = """{"EMAIL":"test@mail.com","WhatsApp":"+57300","LATITUDE":4.6}""";

        string? result = SensitiveDataRedactor.Redact(json);

        Assert.DoesNotContain("test@mail.com", result);
        Assert.DoesNotContain("+57300", result);
        Assert.DoesNotContain("4.6", result);
    }

    [Fact]
    public void Redact_DoesNotModify_NameDescriptionCityProvinceCountry()
    {
        string json = """{"name":"Mi Restaurante","description":"Comida rica","city":"Cali","province":"Valle","country":"Colombia"}""";

        string? result = SensitiveDataRedactor.Redact(json);

        Assert.Contains("Mi Restaurante", result);
        Assert.Contains("Comida rica", result);
        Assert.Contains("Cali", result);
        Assert.Contains("Valle", result);
        Assert.Contains("Colombia", result);
        Assert.DoesNotContain("***REDACTED***", result);
    }

    [Fact]
    public void Redact_InvalidJson_ReturnsOmittedMarker()
    {
        string json = "{not valid json";

        string? result = SensitiveDataRedactor.Redact(json);

        Assert.Equal("[OMITIDO: contenido JSON no válido]", result);
    }

    [Fact]
    public void Redact_EmptyBody_ReturnsEmpty()
    {
        string? result = SensitiveDataRedactor.Redact("");

        Assert.Equal("", result);
    }

    [Fact]
    public void Redact_Null_ReturnsNull()
    {
        string? result = SensitiveDataRedactor.Redact(null);

        Assert.Null(result);
    }
}
