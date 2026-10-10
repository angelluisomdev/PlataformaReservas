using PlataformaReservas.Aplicacion.Abstracciones;

namespace PlataformaReservas.PruebasIntegracion.Infraestructura;

public sealed class ContextoEmpresaAjustable : IContextoEmpresa
{
    public Guid? EmpresaId { get; set; }

    public Guid EmpresaActualId => EmpresaId ?? throw new InvalidOperationException("La prueba no ha fijado la empresa.");

    public bool TieneEmpresa => EmpresaId is not null;
}
