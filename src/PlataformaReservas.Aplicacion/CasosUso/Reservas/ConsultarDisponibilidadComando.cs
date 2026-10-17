namespace PlataformaReservas.Aplicacion.CasosUso.Reservas;

public sealed record ConsultarDisponibilidadComando(Guid EmpresaId, Guid ServicioId, Guid? ProfesionalId, DateOnly Fecha);
