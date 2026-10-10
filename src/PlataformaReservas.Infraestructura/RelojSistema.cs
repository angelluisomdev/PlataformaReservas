using PlataformaReservas.Aplicacion.Abstracciones;

namespace PlataformaReservas.Infraestructura;

public sealed class RelojSistema : IRelojSistema
{
    public DateTime AhoraUtc => DateTime.UtcNow;
}
