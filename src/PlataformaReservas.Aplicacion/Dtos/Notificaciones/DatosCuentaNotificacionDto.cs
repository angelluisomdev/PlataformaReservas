using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.Aplicacion.Dtos.Notificaciones;

public sealed record DatosCuentaNotificacionDto(Guid UsuarioId, string Email, string Nombre, int ReservasCanceladas)
{
    public static DatosCuentaNotificacionDto ConvertirADto(Guid usuarioId, Email email, NombrePersona nombre, int reservasCanceladas)
    {
        return new DatosCuentaNotificacionDto(usuarioId, email.Valor, nombre.Valor, reservasCanceladas);
    }
}
