namespace PlataformaReservas.Aplicacion.Abstracciones;

public interface IContextoEmpresa
{
    Guid EmpresaActualId { get; }

    bool TieneEmpresa { get; }
}
