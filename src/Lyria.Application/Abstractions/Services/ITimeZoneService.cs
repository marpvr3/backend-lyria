namespace Lyria.Application.Abstractions.Services;

public interface ITimeZoneService
{
    bool IsValid(string timeZoneId);
    DateTime ConvertUtcToLocal(DateTimeOffset utcDateTime, string timeZoneId);
}
