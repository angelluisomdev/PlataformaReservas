using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.Dominio.Repositorios;

public interface IRepositorioEmpresas
{
    Task<Empresa?> ObtenerPorIdAsync(Guid id, CancellationToken ct);

    Task<Empresa?> ObtenerParaModificarAsync(Guid id, CancellationToken ct);

    Task<bool> ExisteSlugAsync(Slug slug, CancellationToken ct);

    void Agregar(Empresa empresa);
}
