using PlataformaReservas.Dominio.Eventos;

namespace PlataformaReservas.Aplicacion.Abstracciones;

public interface IManejadorEvento<in TEvento>
    where TEvento : IEventoDominio
{
    Task ManejarAsync(TEvento evento, CancellationToken ct);
}
