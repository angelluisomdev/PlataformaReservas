using PlataformaReservas.Dominio.Entidades;

namespace PlataformaReservas.Aplicacion.Dtos.Profesionales;

public sealed record ProfesionalDisponibilidadDto(
    Guid Id, string NombreCompleto, IReadOnlyList<IntervaloHorarioDto> Horarios, IReadOnlyList<DateOnly> FechasNoDisponibles)
{
    public static ProfesionalDisponibilidadDto ConvertirADto(Profesional profesional)
    {
        return new ProfesionalDisponibilidadDto(
            profesional.Id,
            profesional.NombreCompleto.Valor,
            profesional.Horarios.Select(IntervaloHorarioDto.ConvertirADto).ToList(),
            profesional.Excepciones.Select(x => x.Fecha).ToList());
    }
}
