using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.Aplicacion.Dtos.Notificaciones;

public sealed record DatosReservaNotificacionDto(
    Guid ReservaId,
    string Establecimiento,
    string Direccion,
    string EmailEstablecimiento,
    string Servicio,
    string Profesional,
    DateTime InicioUtc,
    int DuracionMinutos,
    decimal Precio,
    string NombreCliente,
    string TelefonoCliente,
    string EmailCliente)
{
    public static DatosReservaNotificacionDto ConvertirADto(
        Guid reservaId,
        NombreEmpresa establecimiento,
        Direccion direccion,
        CodigoPostal codigoPostal,
        Ciudad ciudad,
        Email emailEstablecimiento,
        NombreServicio servicio,
        NombrePersona profesional,
        FranjaHoraria franja,
        Duracion duracion,
        Precio precio,
        NombrePersona nombreCliente,
        Telefono telefonoCliente,
        Email emailCliente)
    {
        return new DatosReservaNotificacionDto(
            reservaId,
            establecimiento.Valor,
            $"{direccion.Valor}, {codigoPostal.Valor} {ciudad.Valor}",
            emailEstablecimiento.Valor,
            servicio.Valor,
            profesional.Valor,
            franja.InicioUtc,
            duracion.Minutos,
            precio.Importe,
            nombreCliente.Valor,
            telefonoCliente.Valor,
            emailCliente.Valor);
    }
}
