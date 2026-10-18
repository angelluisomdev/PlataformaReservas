using PlataformaReservas.Dominio.Enumeraciones;

namespace PlataformaReservas.Aplicacion.CasosUso.Reservas;

public sealed record ConsultarReservasEmpresaComando(DateOnly Fecha, Guid? ProfesionalId, EstadoReserva? Estado);
