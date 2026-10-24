using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Dtos.Notificaciones;

namespace PlataformaReservas.PruebasUnitarias.Infraestructura;

public sealed class RenderizadorFalso : IRenderizadorPlantillas
{
    public Task<MensajeCorreo> ReservaConfirmadaClienteAsync(DatosReservaNotificacionDto datos) =>
        Mensaje(datos.EmailCliente, nameof(ReservaConfirmadaClienteAsync));

    public Task<MensajeCorreo> ReservaConfirmadaNegocioAsync(DatosReservaNotificacionDto datos) =>
        Mensaje(datos.EmailEstablecimiento, nameof(ReservaConfirmadaNegocioAsync));

    public Task<MensajeCorreo> ReservaCanceladaPorClienteAsync(DatosReservaNotificacionDto datos) =>
        Mensaje(datos.EmailEstablecimiento, nameof(ReservaCanceladaPorClienteAsync));

    public Task<MensajeCorreo> ReservaCanceladaPorEmpresaAsync(DatosReservaNotificacionDto datos) =>
        Mensaje(datos.EmailCliente, nameof(ReservaCanceladaPorEmpresaAsync));

    public Task<MensajeCorreo> ReservaCanceladaPorBajaEmpresaAsync(DatosReservaNotificacionDto datos) =>
        Mensaje(datos.EmailCliente, nameof(ReservaCanceladaPorBajaEmpresaAsync));

    public Task<MensajeCorreo> ReservaCanceladaPorBajaCuentaAsync(DatosReservaNotificacionDto datos) =>
        Mensaje(datos.EmailEstablecimiento, nameof(ReservaCanceladaPorBajaCuentaAsync));

    public Task<MensajeCorreo> EmpresaRegistradaAsync(DatosEmpresaNotificacionDto datos) =>
        Mensaje(datos.EmailPropietario, nameof(EmpresaRegistradaAsync));

    public Task<MensajeCorreo> EmpresaDadaDeBajaAsync(DatosEmpresaNotificacionDto datos) =>
        Mensaje(datos.EmailPropietario, nameof(EmpresaDadaDeBajaAsync));

    public Task<MensajeCorreo> CuentaRegistradaAsync(DatosCuentaNotificacionDto datos) =>
        Mensaje(datos.Email, nameof(CuentaRegistradaAsync));

    public Task<MensajeCorreo> CuentaDadaDeBajaAsync(DatosCuentaNotificacionDto datos) =>
        Mensaje(datos.Email, nameof(CuentaDadaDeBajaAsync));

    public Task<MensajeCorreo> RestablecimientoClaveAsync(string correo, string nombre, string enlace) =>
        Mensaje(correo, nameof(RestablecimientoClaveAsync));

    private static Task<MensajeCorreo> Mensaje(string para, string plantilla)
    {
        return Task.FromResult(new MensajeCorreo(para, plantilla, ""));
    }
}
