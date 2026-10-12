namespace PlataformaReservas.Aplicacion.CasosUso.Reservas;

public sealed record CrearReservaComando(
    Guid EmpresaId, Guid ServicioId, Guid ProfesionalId, DateOnly Fecha, DateTime InicioUtc, string? Observaciones);
