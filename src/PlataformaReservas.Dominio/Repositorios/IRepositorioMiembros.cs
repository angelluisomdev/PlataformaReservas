using PlataformaReservas.Dominio.Entidades;

namespace PlataformaReservas.Aplicacion.Abstracciones;

public interface IRepositorioMiembros
{
    Task<MiembroEmpresa?> ObtenerPorUsuarioAsync(Guid usuarioId, CancellationToken ct);
}
