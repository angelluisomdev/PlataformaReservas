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
    bool Cancelable,
    string? MotivoNoCancelable)
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
        bool cancelable = Reserva.EsCancelablePorCliente(estado, franja, ahoraUtc);
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
            cancelable,
            cancelable ? null : MotivoNoCancelablePor(estado));
    }

    private static string MotivoNoCancelablePor(EstadoReserva estado) => estado switch
    {
        EstadoReserva.Confirmada => "La reserva ya ha comenzado y no se puede cancelar desde aquí. Contacta con el establecimiento.",
        EstadoReserva.Cancelada => "Esta reserva está cancelada y ya no se puede cancelar.",
        _ => "Esta reserva está completada y ya no se puede cancelar.",
    };
}
