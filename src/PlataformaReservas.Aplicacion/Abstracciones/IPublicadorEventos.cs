using PlataformaReservas.Dominio.Eventos;

namespace PlataformaReservas.Aplicacion.Abstracciones;

public interface IPublicadorEventos
{
    ValueTask PublicarAsync(IReadOnlyCollection<IEventoDominio> eventos, CancellationToken ct);
}
