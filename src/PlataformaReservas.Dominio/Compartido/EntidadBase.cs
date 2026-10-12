using PlataformaReservas.Dominio.Eventos;

namespace PlataformaReservas.Dominio.Compartido;

public abstract class EntidadBase
{
    private readonly List<IEventoDominio> _eventos = [];

    public Guid Id { get; protected set; }

    public IReadOnlyCollection<IEventoDominio> Eventos => _eventos.AsReadOnly();

    public void LimpiarEventos()
    {
        _eventos.Clear();
    }

    protected void RegistrarEvento(IEventoDominio evento)
    {
        _eventos.Add(evento);
    }
}
