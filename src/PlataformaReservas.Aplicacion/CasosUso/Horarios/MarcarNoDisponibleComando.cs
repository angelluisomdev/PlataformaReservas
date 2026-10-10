namespace PlataformaReservas.Aplicacion.CasosUso.Horarios;

public sealed record MarcarNoDisponibleComando(Guid ProfesionalId, DateOnly Fecha, string? Motivo);
