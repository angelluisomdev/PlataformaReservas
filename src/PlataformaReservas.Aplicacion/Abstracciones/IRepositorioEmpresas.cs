using PlataformaReservas.Dominio.Entidades;

namespace PlataformaReservas.Aplicacion.Abstracciones;

public interface IRepositorioEmpresas
{
    Task<Empresa?> ObtenerPorIdAsync(Guid id, CancellationToken ct);

    Task<Empresa?> ObtenerParaModificarAsync(Guid id, CancellationToken ct);
}
