using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.Enumeraciones;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.Aplicacion.Dtos.Reservas;

public sealed record DetalleReservaClienteDto(
    Guid Id,
    string Establecimiento,
    string Servicio,
    string Profesional,
    DateTime InicioUtc,
    DateTime FinUtc,
    int DuracionMinutos,
    decimal PrecioAplicado,
    string? Observaciones,
    EstadoReserva Estado,
    bool Cancelable)
{
    public static DetalleReservaClienteDto ConvertirADto(
        Reserva reserva, string establecimiento, string servicio, string profesional, DateTime ahoraUtc)
    {
        return ConvertirADto(
            reserva.Id, establecimiento, servicio, profesional, reserva.Franja, reserva.DuracionAplicada,
            reserva.PrecioAplicado, reserva.Observaciones, reserva.Estado, ahoraUtc);
    }

    public static DetalleReservaClienteDto ConvertirADto(
        Guid id,
        string establecimiento,
        string servicio,
        string profesional,
        FranjaHoraria franja,
        Duracion duracion,
        Precio precio,
        Observaciones? observaciones,
        EstadoReserva estado,
        DateTime ahoraUtc)
    {
        return new DetalleReservaClienteDto(
            id,
            establecimiento,
            servicio,
            profesional,
            franja.InicioUtc,
            franja.FinUtc,
            duracion.Minutos,
            precio.Importe,
            observaciones?.Valor,
            estado,
            Reserva.EsCancelablePorCliente(estado, franja, ahoraUtc));
    }
}
