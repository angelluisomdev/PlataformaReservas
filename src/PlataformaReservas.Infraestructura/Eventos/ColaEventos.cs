using System.Threading.Channels;
using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Dominio.Eventos;

namespace PlataformaReservas.Infraestructura.Eventos;

public sealed class ColaEventos : IPublicadorEventos
{
    private readonly Channel<IEventoDominio> _canal =
        Channel.CreateUnbounded<IEventoDominio>(new UnboundedChannelOptions { SingleReader = true });

    public async ValueTask PublicarAsync(IReadOnlyCollection<IEventoDominio> eventos, CancellationToken ct)
    {
        foreach (IEventoDominio evento in eventos)
        {
            await _canal.Writer.WriteAsync(evento, ct);
        }
    }

    public IAsyncEnumerable<IEventoDominio> LeerAsync(CancellationToken ct)
    {
        return _canal.Reader.ReadAllAsync(ct);
    }
}
