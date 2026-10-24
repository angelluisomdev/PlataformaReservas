using PlataformaReservas.Aplicacion.Dtos.Notificaciones;

namespace PlataformaReservas.Aplicacion.Abstracciones;

public interface IConsultasNotificacion
{
    Task<DatosReservaNotificacionDto?> ObtenerDatosReservaAsync(Guid reservaId, CancellationToken ct);

    Task<DatosEmpresaNotificacionDto?> ObtenerDatosEmpresaAsync(Guid empresaId, DateTime instanteEvento, CancellationToken ct);

    Task<DatosCuentaNotificacionDto?> ObtenerDatosCuentaAsync(Guid usuarioId, DateTime instanteEvento, CancellationToken ct);
}
