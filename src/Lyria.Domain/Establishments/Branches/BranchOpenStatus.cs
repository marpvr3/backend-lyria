namespace Lyria.Domain.Establishments.Branches;

/// <summary>
/// Estado de apertura de una sede.
/// </summary>
public enum BranchOpenStatus : byte
{
    /// <summary>La sede está abierta.</summary>
    Open = 1,

    /// <summary>La sede abre más tarde hoy.</summary>
    OpensLaterToday = 2,

    /// <summary>La sede está cerrada.</summary>
    Closed = 3,

    /// <summary>No existe programación para la fecha.</summary>
    NoSchedule = 4
}
