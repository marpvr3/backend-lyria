using Lyria.Application.Abstractions.Services;
using Lyria.Infrastructure.Services;
using Xunit;

namespace Lyria.Infrastructure.IntegrationTests.Services;

public sealed class TimeZoneServiceTests
{
    private readonly TimeZoneService _sut = new();

    // --- IsValid ---

    [Fact]
    public void IsValid_BuenosAires_ReturnsTrue()
    {
        bool result = _sut.IsValid("America/Argentina/Buenos_Aires");

        Assert.True(result);
    }

    [Fact]
    public void IsValid_Bogota_ReturnsTrue()
    {
        bool result = _sut.IsValid("America/Bogota");

        Assert.True(result);
    }

    [Fact]
    public void IsValid_Utc_ReturnsTrue()
    {
        bool result = _sut.IsValid("UTC");

        Assert.True(result);
    }

    [Fact]
    public void IsValid_InvalidZone_ReturnsFalse()
    {
        bool result = _sut.IsValid("Invalid/TimeZone");

        Assert.False(result);
    }

    [Fact]
    public void IsValid_EmptyString_ReturnsFalse()
    {
        bool result = _sut.IsValid("");

        Assert.False(result);
    }

    // --- ConvertUtcToLocal ---

    [Fact]
    public void ConvertUtcToLocal_BuenosAires_ReturnsCorrectOffset()
    {
        // Buenos Aires is UTC-3 (no DST)
        var utc = new DateTimeOffset(2026, 7, 25, 15, 0, 0, TimeSpan.Zero);

        DateTime local = _sut.ConvertUtcToLocal(utc, "America/Argentina/Buenos_Aires");

        Assert.Equal(new DateTime(2026, 7, 25, 12, 0, 0), local);
    }

    [Fact]
    public void ConvertUtcToLocal_Bogota_ReturnsCorrectOffset()
    {
        // Bogota is UTC-5 (no DST)
        var utc = new DateTimeOffset(2026, 7, 25, 15, 0, 0, TimeSpan.Zero);

        DateTime local = _sut.ConvertUtcToLocal(utc, "America/Bogota");

        Assert.Equal(new DateTime(2026, 7, 25, 10, 0, 0), local);
    }

    [Fact]
    public void ConvertUtcToLocal_SameInputDifferentZones_ProducesDifferentResults()
    {
        var utc = new DateTimeOffset(2026, 1, 1, 3, 0, 0, TimeSpan.Zero);

        DateTime buenosAires = _sut.ConvertUtcToLocal(utc, "America/Argentina/Buenos_Aires");
        DateTime bogota = _sut.ConvertUtcToLocal(utc, "America/Bogota");

        Assert.NotEqual(buenosAires, bogota);
        Assert.Equal(2, (buenosAires - bogota).TotalHours);
    }

    [Fact]
    public void ConvertUtcToLocal_Deterministic_SameInputSameOutput()
    {
        var utc = new DateTimeOffset(2026, 6, 15, 18, 30, 0, TimeSpan.Zero);

        DateTime first = _sut.ConvertUtcToLocal(utc, "America/Argentina/Buenos_Aires");
        DateTime second = _sut.ConvertUtcToLocal(utc, "America/Argentina/Buenos_Aires");

        Assert.Equal(first, second);
    }

    [Fact]
    public void ConvertUtcToLocal_DoesNotDependOnLocalTimezone()
    {
        // Regardless of the server's local timezone, conversion should be consistent
        var utc = new DateTimeOffset(2026, 3, 15, 0, 0, 0, TimeSpan.Zero);

        DateTime result = _sut.ConvertUtcToLocal(utc, "America/Bogota");

        // Bogota is UTC-5, so March 15 00:00 UTC = March 14 19:00 Bogota
        Assert.Equal(new DateTime(2026, 3, 14, 19, 0, 0), result);
    }

    [Fact]
    public void ConvertUtcToLocal_InvalidZone_ThrowsTimeZoneNotFoundException()
    {
        var utc = new DateTimeOffset(2026, 7, 25, 15, 0, 0, TimeSpan.Zero);

        Assert.Throws<TimeZoneNotFoundException>(
            () => _sut.ConvertUtcToLocal(utc, "Invalid/Zone"));
    }

    [Fact]
    public void ConvertUtcToLocal_ResultIsNotLocalServerTime()
    {
        // Verify that conversion uses the specified timezone, not the server's local timezone.
        // If local is not Buenos Aires, this proves independence. If local IS Buenos Aires,
        // the Bogota test already proves independence since Bogota differs from local.
        var utc = new DateTimeOffset(2026, 7, 25, 12, 0, 0, TimeSpan.Zero);

        DateTime buenosAires = _sut.ConvertUtcToLocal(utc, "America/Argentina/Buenos_Aires");
        DateTime bogota = _sut.ConvertUtcToLocal(utc, "America/Bogota");

        // Buenos Aires = 09:00, Bogota = 07:00. At least one must differ from server local time.
        Assert.Equal(new DateTime(2026, 7, 25, 9, 0, 0), buenosAires);
        Assert.Equal(new DateTime(2026, 7, 25, 7, 0, 0), bogota);
    }
}
