using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.Aplicacion.Dtos.Empresas;

public sealed record HorarioDiaDto(DayOfWeek Dia, IReadOnlyList<IntervaloHorario> Intervalos)
{
    private static readonly DayOfWeek[] Semana =
    [
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
        DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday,
    ];

    public bool Cerrado => Intervalos.Count == 0;

    public static IReadOnlyList<HorarioDiaDto> ConvertirADto(IEnumerable<Horario> horarios)
    {
        ILookup<DayOfWeek, IntervaloHorario> porDia = horarios.ToLookup(h => h.DiaSemana, h => h.Intervalo);
        return Semana.Select(dia => new HorarioDiaDto(dia, Unir(porDia[dia]))).ToList();
    }

    private static List<IntervaloHorario> Unir(IEnumerable<IntervaloHorario> intervalos)
    {
        List<IntervaloHorario> unidos = [];
        foreach (IntervaloHorario intervalo in intervalos.OrderBy(i => i.HoraInicio))
        {
            IntervaloHorario? ultimo = unidos.Count > 0 ? unidos[^1] : null;
            if (ultimo is not null && (ultimo.SeSolapaCon(intervalo) || ultimo.HoraFin == intervalo.HoraInicio))
            {
                TimeOnly fin = intervalo.HoraFin > ultimo.HoraFin ? intervalo.HoraFin : ultimo.HoraFin;
                unidos[^1] = new IntervaloHorario(ultimo.HoraInicio, fin);
            }
            else
            {
                unidos.Add(intervalo);
            }
        }

        return unidos;
    }
}
