using PlataformaReservas.Aplicacion.Abstracciones;

namespace PlataformaReservas.PruebasIntegracion.Infraestructura;

public sealed class ContextoEmpresaFijo(Guid empresaId) : IContextoEmpresa
{
    public Guid EmpresaActualId => empresaId;

    public bool TieneEmpresa => true;
}
