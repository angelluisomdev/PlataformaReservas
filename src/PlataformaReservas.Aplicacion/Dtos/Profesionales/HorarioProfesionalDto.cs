using PlataformaReservas.Dominio.Entidades;

namespace PlataformaReservas.Aplicacion.Dtos.Profesionales;

public sealed record HorarioProfesionalDto(
    Guid ProfesionalId, IReadOnlyList<IntervaloHorarioDto> Intervalos, IReadOnlyList<ExcepcionHorarioDto> Excepciones)
{
    public static HorarioProfesionalDto ConvertirADto(Profesional profesional)
    {
        List<IntervaloHorarioDto> intervalos = profesional.Horarios
            .OrderBy(h => ((int)h.DiaSemana + 6) % 7)
            .ThenBy(h => h.Intervalo.HoraInicio)
            .Select(IntervaloHorarioDto.ConvertirADto)
            .ToList();
        List<ExcepcionHorarioDto> excepciones = profesional.Excepciones
            .OrderBy(x => x.Fecha)
            .Select(ExcepcionHorarioDto.ConvertirADto)
            .ToList();

        return new HorarioProfesionalDto(profesional.Id, intervalos, excepciones);
    }
}
