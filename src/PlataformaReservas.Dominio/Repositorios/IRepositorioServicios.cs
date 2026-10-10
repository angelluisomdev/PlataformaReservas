using PlataformaReservas.Dominio.Entidades;

namespace PlataformaReservas.Aplicacion.Abstracciones;

public interface IRepositorioServicios
{
    Task<Servicio?> ObtenerPorIdAsync(Guid id, CancellationToken ct);

    Task<Servicio?> ObtenerParaModificarAsync(Guid id, CancellationToken ct);
}
