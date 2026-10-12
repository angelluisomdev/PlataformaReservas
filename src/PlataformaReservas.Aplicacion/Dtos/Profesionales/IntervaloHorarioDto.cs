namespace PlataformaReservas.Aplicacion.Dtos;

public sealed record IntervaloHorarioDto(Guid Id, DayOfWeek DiaSemana, TimeOnly HoraInicio, TimeOnly HoraFin);
