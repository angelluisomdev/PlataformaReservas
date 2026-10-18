using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.Enumeraciones;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.Aplicacion.Dtos.Reservas;

public sealed record ReservaEmpresaDto(
    Guid Id,
    DateTime InicioUtc,
    DateTime FinUtc,
    string Servicio,
    string Profesional,
    string NombreCliente,
    string TelefonoCliente,
    string? Observaciones,
    EstadoReserva Estado,
    bool Cancelable,
    bool Completable)
{
    public static ReservaEmpresaDto ConvertirADto(Reserva reserva, string servicio, string profesional, DateTime ahoraUtc)
    {
        return ConvertirADto(
            reserva.Id, reserva.Franja, servicio, profesional, reserva.NombreCliente, reserva.TelefonoCliente,
            reserva.Observaciones, reserva.Estado, ahoraUtc);
    }

    public static ReservaEmpresaDto ConvertirADto(
        Guid id,
        FranjaHoraria franja,
        string servicio,
        string profesional,
        NombrePersona nombreCliente,
        Telefono telefonoCliente,
        Observaciones? observaciones,
        EstadoReserva estado,
        DateTime ahoraUtc)
    {
        return new ReservaEmpresaDto(
            id,
            franja.InicioUtc,
            franja.FinUtc,
            servicio,
            profesional,
            nombreCliente.Valor,
            telefonoCliente.Valor,
            observaciones?.Valor,
            estado,
            estado == EstadoReserva.Confirmada,
            Reserva.EsCompletable(estado, franja, ahoraUtc));
    }
}
