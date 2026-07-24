using Lyria.Domain.Establishments.Branches;

namespace Lyria.Application.Features.BranchSchedules;

public static class WeekDayNames
{
    public static string GetSpanishName(WeekDay day) => day switch
    {
        WeekDay.Monday => "Lunes",
        WeekDay.Tuesday => "Martes",
        WeekDay.Wednesday => "Miércoles",
        WeekDay.Thursday => "Jueves",
        WeekDay.Friday => "Viernes",
        WeekDay.Saturday => "Sábado",
        WeekDay.Sunday => "Domingo",
        _ => day.ToString()
    };
}
