using Lyria.Application.Abstractions.Services;

namespace Lyria.Infrastructure.Services;

/// <summary>
/// Servicio de zona horaria que almacena y recibe identificadores IANA.
/// <para>
/// Estrategia multiplataforma:
/// <list type="bullet">
///   <item>.NET 10 en macOS/Linux: <c>FindSystemTimeZoneById</c> resuelve IANA directamente.</item>
///   <item>.NET 10 en Windows con ICU: <c>FindSystemTimeZoneById</c> resuelve IANA directamente.</item>
///   <item>.NET 10 en Windows/IIS sin ICU: fallback a <c>TryConvertIanaIdToWindowsId</c>
///         para traducir el identificador IANA a Windows antes de resolver.</item>
/// </list>
/// No depende de <c>TimeZoneInfo.Local</c> ni de la zona horaria del servidor.
/// No almacena identificadores Windows; siempre trabaja con IANA como formato canónico.
/// </para>
/// </summary>
internal sealed class TimeZoneService : ITimeZoneService
{
    public bool IsValid(string timeZoneId)
    {
        return FindTimeZone(timeZoneId) is not null;
    }

    public DateTime ConvertUtcToLocal(DateTimeOffset utcDateTime, string timeZoneId)
    {
        TimeZoneInfo timeZone = FindTimeZone(timeZoneId)
            ?? throw new TimeZoneNotFoundException(
                $"La zona horaria '{timeZoneId}' no se pudo resolver en esta plataforma.");

        return TimeZoneInfo.ConvertTime(utcDateTime, timeZone).DateTime;
    }

    private static TimeZoneInfo? FindTimeZone(string timeZoneId)
    {
        // Intento directo: funciona en macOS/Linux y en Windows con ICU
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            // Fallback para Windows/IIS sin ICU: traducir IANA a Windows ID
        }

        if (TimeZoneInfo.TryConvertIanaIdToWindowsId(timeZoneId, out string? windowsId))
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(windowsId);
            }
            catch (TimeZoneNotFoundException)
            {
                // El ID Windows tampoco está disponible
            }
        }

        return null;
    }
}
