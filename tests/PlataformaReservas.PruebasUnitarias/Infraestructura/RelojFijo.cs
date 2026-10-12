using PlataformaReservas.Aplicacion.Abstracciones;

namespace PlataformaReservas.PruebasUnitarias.Infraestructura;

public sealed class RelojFijo(DateTime ahoraUtc) : IRelojSistema
{
    public DateTime AhoraUtc { get; } = ahoraUtc;
}
