using PlataformaReservas.Aplicacion.Abstracciones;

namespace PlataformaReservas.PruebasIntegracion.Infraestructura;

public sealed class RelojFijo : IRelojSistema
{
    public DateTime AhoraUtc => DominioPrueba.Ahora;
}
