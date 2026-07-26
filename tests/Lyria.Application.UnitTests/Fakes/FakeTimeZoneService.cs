using Lyria.Application.Abstractions.Services;

namespace Lyria.Application.UnitTests.Fakes;

internal sealed class FakeTimeZoneService : ITimeZoneService
{
    private bool _isValid = true;

    public void SetValid(bool isValid) => _isValid = isValid;

    public bool IsValid(string timeZoneId) => _isValid;

    public DateTime ConvertUtcToLocal(DateTimeOffset utcDateTime, string timeZoneId) =>
        utcDateTime.UtcDateTime;
}
