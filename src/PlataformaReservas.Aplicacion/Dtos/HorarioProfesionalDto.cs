namespace PlataformaReservas.Aplicacion.Dtos;

public sealed record HorarioProfesionalDto(
    Guid ProfesionalId, IReadOnlyList<IntervaloHorarioDto> Intervalos, IReadOnlyList<ExcepcionHorarioDto> Excepciones);
