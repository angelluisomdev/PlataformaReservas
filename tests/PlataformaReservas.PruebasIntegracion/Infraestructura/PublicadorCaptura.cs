using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Dominio.Eventos;

namespace PlataformaReservas.PruebasIntegracion.Infraestructura;

public sealed class PublicadorCaptura : IPublicadorEventos
{
    private readonly List<IEventoDominio> _publicados = [];

    public bool Fallar { get; set; }

    public IReadOnlyList<IEventoDominio> Publicados => _publicados;

    public ValueTask PublicarAsync(IReadOnlyCollection<IEventoDominio> eventos, CancellationToken ct)
    {
        if (Fallar)
        {
            throw new InvalidOperationException("El publicador falla a proposito.");
        }

        _publicados.AddRange(eventos);
        return ValueTask.CompletedTask;
    }
}
