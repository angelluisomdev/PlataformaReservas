using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Dtos.Notificaciones;
using PlataformaReservas.Dominio.Eventos;

namespace PlataformaReservas.Aplicacion.Notificaciones;

public sealed class EnviarCorreosReservaConfirmada(
    IConsultasNotificacion consultas, IRenderizadorPlantillas plantillas, IServicioEmail correo)
    : IManejadorEvento<ReservaConfirmada>
{
    public async Task ManejarAsync(ReservaConfirmada evento, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(evento);
        DatosReservaNotificacionDto? datos = await consultas.ObtenerDatosReservaAsync(evento.ReservaId, ct);
        if (datos is null)
        {
            return;
        }

        await correo.EnviarAsync(await plantillas.ReservaConfirmadaClienteAsync(datos), ct);
        await correo.EnviarAsync(await plantillas.ReservaConfirmadaNegocioAsync(datos), ct);
    }
}
