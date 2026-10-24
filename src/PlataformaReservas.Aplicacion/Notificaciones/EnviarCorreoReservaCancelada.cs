using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Dtos.Notificaciones;
using PlataformaReservas.Dominio.Enumeraciones;
using PlataformaReservas.Dominio.Eventos;

namespace PlataformaReservas.Aplicacion.Notificaciones;

public sealed class EnviarCorreoReservaCancelada(
    IConsultasNotificacion consultas, IRenderizadorPlantillas plantillas, IServicioEmail correo)
    : IManejadorEvento<ReservaCancelada>
{
    public async Task ManejarAsync(ReservaCancelada evento, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(evento);
        DatosReservaNotificacionDto? datos = await consultas.ObtenerDatosReservaAsync(evento.ReservaId, ct);
        if (datos is null)
        {
            return;
        }

        MensajeCorreo mensaje = evento.Origen switch
        {
            OrigenCancelacion.Cliente => await plantillas.ReservaCanceladaPorClienteAsync(datos),
            OrigenCancelacion.Empresa => await plantillas.ReservaCanceladaPorEmpresaAsync(datos),
            OrigenCancelacion.BajaEmpresa => await plantillas.ReservaCanceladaPorBajaEmpresaAsync(datos),
            OrigenCancelacion.BajaCuenta => await plantillas.ReservaCanceladaPorBajaCuentaAsync(datos),
            _ => throw new ArgumentOutOfRangeException(nameof(evento), evento.Origen, "Origen de cancelacion desconocido."),
        };
        await correo.EnviarAsync(mensaje, ct);
    }
}
