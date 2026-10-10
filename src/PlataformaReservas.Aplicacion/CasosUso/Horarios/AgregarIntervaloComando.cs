namespace PlataformaReservas.Aplicacion.CasosUso.Horarios;

public sealed record AgregarIntervaloComando(Guid ProfesionalId, DayOfWeek DiaSemana, TimeOnly HoraInicio, TimeOnly HoraFin);
