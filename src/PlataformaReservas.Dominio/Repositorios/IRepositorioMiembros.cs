using PlataformaReservas.Dominio.Entidades;

namespace PlataformaReservas.Dominio.Repositorios;

public interface IRepositorioMiembros
{
    Task<MiembroEmpresa?> ObtenerPorUsuarioAsync(Guid usuarioId, CancellationToken ct);

    void Agregar(MiembroEmpresa miembro);
}
