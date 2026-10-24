using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Dtos.Notificaciones;
using PlataformaReservas.Dominio.Eventos;

namespace PlataformaReservas.Aplicacion.Notificaciones;

public sealed class EnviarCorreoEmpresaDadaDeBaja(
    IConsultasNotificacion consultas, IRenderizadorPlantillas plantillas, IServicioEmail correo)
    : IManejadorEvento<EmpresaDadaDeBaja>
{
    public async Task ManejarAsync(EmpresaDadaDeBaja evento, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(evento);
        DatosEmpresaNotificacionDto? datos = await consultas.ObtenerDatosEmpresaAsync(evento.EmpresaId, evento.OcurridoEnUtc, ct);
        if (datos is null)
        {
            return;
        }

        await correo.EnviarAsync(await plantillas.EmpresaDadaDeBajaAsync(datos), ct);
    }
}
