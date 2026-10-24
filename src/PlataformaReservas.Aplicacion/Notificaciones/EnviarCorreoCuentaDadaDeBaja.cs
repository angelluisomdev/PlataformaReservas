using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Dtos.Notificaciones;
using PlataformaReservas.Dominio.Eventos;

namespace PlataformaReservas.Aplicacion.Notificaciones;

public sealed class EnviarCorreoCuentaDadaDeBaja(
    IConsultasNotificacion consultas, IRenderizadorPlantillas plantillas, IServicioEmail correo)
    : IManejadorEvento<CuentaDadaDeBaja>
{
    public async Task ManejarAsync(CuentaDadaDeBaja evento, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(evento);
        DatosCuentaNotificacionDto? datos = await consultas.ObtenerDatosCuentaAsync(evento.UsuarioId, evento.OcurridoEnUtc, ct);
        if (datos is null)
        {
            return;
        }

        await correo.EnviarAsync(await plantillas.CuentaDadaDeBajaAsync(datos), ct);
    }
}
