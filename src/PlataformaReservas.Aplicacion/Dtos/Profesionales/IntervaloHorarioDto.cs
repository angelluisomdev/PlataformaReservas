using PlataformaReservas.Dominio.Entidades;

namespace PlataformaReservas.Aplicacion.Dtos.Profesionales;

public sealed record IntervaloHorarioDto(Guid Id, DayOfWeek DiaSemana, TimeOnly HoraInicio, TimeOnly HoraFin)
{
    public static IntervaloHorarioDto ConvertirADto(Horario horario)
    {
        return new IntervaloHorarioDto(horario.Id, horario.DiaSemana, horario.Intervalo.HoraInicio, horario.Intervalo.HoraFin);
    }
}
