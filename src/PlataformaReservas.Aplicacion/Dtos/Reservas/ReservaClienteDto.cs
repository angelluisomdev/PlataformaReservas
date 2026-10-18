using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.Enumeraciones;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.Aplicacion.Dtos.Reservas;

public sealed record ReservaClienteDto(
    Guid Id, string Establecimiento, string Servicio, string Profesional, DateTime InicioUtc, EstadoReserva Estado, bool Cancelable)
{
    public static ReservaClienteDto ConvertirADto(
        Reserva reserva, string establecimiento, string servicio, string profesional, DateTime ahoraUtc)
    {
        return ConvertirADto(reserva.Id, establecimiento, servicio, profesional, reserva.Franja, reserva.Estado, ahoraUtc);
    }

    public static ReservaClienteDto ConvertirADto(
        Guid id, string establecimiento, string servicio, string profesional, FranjaHoraria franja, EstadoReserva estado,
        DateTime ahoraUtc)
    {
        return new ReservaClienteDto(
            id, establecimiento, servicio, profesional, franja.InicioUtc, estado,
            Reserva.EsCancelablePorCliente(estado, franja, ahoraUtc));
    }
}
