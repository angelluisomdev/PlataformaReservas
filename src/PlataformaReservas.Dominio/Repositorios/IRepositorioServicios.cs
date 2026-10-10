using PlataformaReservas.Dominio.Entidades;

namespace PlataformaReservas.Dominio.Repositorios;

public interface IRepositorioServicios
{
    Task<Servicio?> ObtenerPorIdAsync(Guid id, CancellationToken ct);

    Task<Servicio?> ObtenerParaModificarAsync(Guid id, CancellationToken ct);

    Task<IReadOnlyList<Servicio>> ListarAsync(CancellationToken ct);

    void Agregar(Servicio servicio);
}
