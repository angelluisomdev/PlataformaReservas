using PlataformaReservas.Aplicacion.Dtos.Notificaciones;

namespace PlataformaReservas.Aplicacion.Abstracciones;

public interface IRenderizadorPlantillas
{
    Task<MensajeCorreo> ReservaConfirmadaClienteAsync(DatosReservaNotificacionDto datos);

    Task<MensajeCorreo> ReservaConfirmadaNegocioAsync(DatosReservaNotificacionDto datos);

    Task<MensajeCorreo> ReservaCanceladaPorClienteAsync(DatosReservaNotificacionDto datos);

    Task<MensajeCorreo> ReservaCanceladaPorEmpresaAsync(DatosReservaNotificacionDto datos);

    Task<MensajeCorreo> ReservaCanceladaPorBajaEmpresaAsync(DatosReservaNotificacionDto datos);

    Task<MensajeCorreo> ReservaCanceladaPorBajaCuentaAsync(DatosReservaNotificacionDto datos);

    Task<MensajeCorreo> EmpresaRegistradaAsync(DatosEmpresaNotificacionDto datos);

    Task<MensajeCorreo> EmpresaDadaDeBajaAsync(DatosEmpresaNotificacionDto datos);

    Task<MensajeCorreo> CuentaRegistradaAsync(DatosCuentaNotificacionDto datos);

    Task<MensajeCorreo> CuentaDadaDeBajaAsync(DatosCuentaNotificacionDto datos);

    Task<MensajeCorreo> RestablecimientoClaveAsync(string correo, string nombre, string enlace);
}
