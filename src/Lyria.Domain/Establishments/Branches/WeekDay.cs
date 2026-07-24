namespace Lyria.Domain.Establishments.Branches;

/// <summary>
/// Días de la semana para la programación de horarios de una sede.
/// </summary>
public enum WeekDay : byte
{
    /// <summary>Lunes.</summary>
    Monday = 1,

    /// <summary>Martes.</summary>
    Tuesday = 2,

    /// <summary>Miércoles.</summary>
    Wednesday = 3,

    /// <summary>Jueves.</summary>
    Thursday = 4,

    /// <summary>Viernes.</summary>
    Friday = 5,

    /// <summary>Sábado.</summary>
    Saturday = 6,

    /// <summary>Domingo.</summary>
    Sunday = 7
}
