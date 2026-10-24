using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.HtmlRendering;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Dtos.Notificaciones;
using PlataformaReservas.Infraestructura.Correo.Plantillas;

namespace PlataformaReservas.Infraestructura.Correo;

public sealed class RenderizadorPlantillas(IServiceProvider servicios, ILoggerFactory registros, IConfiguration configuracion)
    : IRenderizadorPlantillas, IAsyncDisposable
{
    private readonly HtmlRenderer _renderizador = new(servicios, registros);
    private readonly IConfiguration _configuracion = configuracion;

    public Task<MensajeCorreo> ReservaConfirmadaClienteAsync(DatosReservaNotificacionDto datos)
    {
        ArgumentNullException.ThrowIfNull(datos);
        return MensajeAsync<ReservaConfirmadaCliente>(
            datos.EmailCliente, $"Reserva confirmada en {datos.Establecimiento}", Parametros(datos));
    }

    public Task<MensajeCorreo> ReservaConfirmadaNegocioAsync(DatosReservaNotificacionDto datos)
    {
        ArgumentNullException.ThrowIfNull(datos);
        return MensajeAsync<ReservaConfirmadaNegocio>(
            datos.EmailEstablecimiento, $"Nueva reserva: {datos.Servicio} el {FormatoCorreo.Fecha(datos.InicioUtc)}", Parametros(datos));
    }

    public Task<MensajeCorreo> ReservaCanceladaPorClienteAsync(DatosReservaNotificacionDto datos)
    {
        ArgumentNullException.ThrowIfNull(datos);
        return MensajeAsync<ReservaCanceladaPorCliente>(
            datos.EmailEstablecimiento, $"Reserva cancelada por el cliente: {datos.Servicio} el {FormatoCorreo.Fecha(datos.InicioUtc)}",
            Parametros(datos));
    }

    public Task<MensajeCorreo> ReservaCanceladaPorEmpresaAsync(DatosReservaNotificacionDto datos)
    {
        ArgumentNullException.ThrowIfNull(datos);
        return MensajeAsync<ReservaCanceladaPorEmpresa>(
            datos.EmailCliente, $"{datos.Establecimiento} ha cancelado tu reserva", Parametros(datos));
    }

    public Task<MensajeCorreo> ReservaCanceladaPorBajaEmpresaAsync(DatosReservaNotificacionDto datos)
    {
        ArgumentNullException.ThrowIfNull(datos);
        return MensajeAsync<ReservaCanceladaPorBajaEmpresa>(
            datos.EmailCliente, $"Tu cita en {datos.Establecimiento} queda anulada", Parametros(datos));
    }

    public Task<MensajeCorreo> ReservaCanceladaPorBajaCuentaAsync(DatosReservaNotificacionDto datos)
    {
        ArgumentNullException.ThrowIfNull(datos);
        return MensajeAsync<ReservaCanceladaPorBajaCuenta>(
            datos.EmailEstablecimiento, $"Cita anulada por baja del cliente: {datos.Servicio} el {FormatoCorreo.Fecha(datos.InicioUtc)}",
            Parametros(datos));
    }

    public Task<MensajeCorreo> EmpresaRegistradaAsync(DatosEmpresaNotificacionDto datos)
    {
        ArgumentNullException.ThrowIfNull(datos);
        return MensajeAsync<EmpresaRegistrada>(
            datos.EmailPropietario, $"{datos.Nombre} ya está registrado", ParametrosConEnlaces(datos));
    }

    public Task<MensajeCorreo> EmpresaDadaDeBajaAsync(DatosEmpresaNotificacionDto datos)
    {
        ArgumentNullException.ThrowIfNull(datos);
        return MensajeAsync<EmpresaDadaDeBaja>(
            datos.EmailPropietario, $"{datos.Nombre} se ha dado de baja", ParametrosConEnlaces(datos));
    }

    public Task<MensajeCorreo> CuentaRegistradaAsync(DatosCuentaNotificacionDto datos)
    {
        ArgumentNullException.ThrowIfNull(datos);
        return MensajeAsync<CuentaRegistrada>(datos.Email, "Te damos la bienvenida", Parametros(datos));
    }

    public Task<MensajeCorreo> CuentaDadaDeBajaAsync(DatosCuentaNotificacionDto datos)
    {
        ArgumentNullException.ThrowIfNull(datos);
        return MensajeAsync<CuentaDadaDeBaja>(datos.Email, "Tu cuenta se ha dado de baja", Parametros(datos));
    }

    public Task<MensajeCorreo> RestablecimientoClaveAsync(string correo, string nombre, string enlace)
    {
        return MensajeAsync<RestablecimientoClave>(
            correo, "Restablece tu contraseña", new Dictionary<string, object?> { ["Nombre"] = nombre, ["Enlace"] = enlace });
    }

    public ValueTask DisposeAsync()
    {
        return _renderizador.DisposeAsync();
    }

    private static Dictionary<string, object?> Parametros(object datos)
    {
        return new Dictionary<string, object?> { ["Datos"] = datos };
    }

    private Dictionary<string, object?> ParametrosConEnlaces(object datos)
    {
        if (!UrlBaseCorreo.IntentarObtener(_configuracion, out string urlBase))
        {
            throw new InvalidOperationException($"{UrlBaseCorreo.Clave} no es una URL http(s) absoluta: no se puede montar el enlace.");
        }

        return new Dictionary<string, object?> { ["Datos"] = datos, ["UrlBase"] = urlBase };
    }

    private async Task<MensajeCorreo> MensajeAsync<TPlantilla>(string para, string asunto, Dictionary<string, object?> parametros)
        where TPlantilla : IComponent
    {
        string html = await _renderizador.Dispatcher.InvokeAsync(async () =>
        {
            HtmlRootComponent raiz = await _renderizador.RenderComponentAsync<TPlantilla>(ParameterView.FromDictionary(parametros));
            return raiz.ToHtmlString();
        });
        return new MensajeCorreo(para, asunto, html);
    }
}
