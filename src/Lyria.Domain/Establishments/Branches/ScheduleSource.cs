namespace Lyria.Domain.Establishments.Branches;

/// <summary>
/// Origen de la programación utilizada para determinar el estado de apertura.
/// </summary>
public enum ScheduleSource : byte
{
    /// <summary>Horario semanal.</summary>
    Weekly = 1,

    /// <summary>Horario especial.</summary>
    Special = 2,

    /// <summary>Sin programación.</summary>
    None = 3
}
