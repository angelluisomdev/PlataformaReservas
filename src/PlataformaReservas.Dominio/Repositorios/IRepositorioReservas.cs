using PlataformaReservas.Dominio.Entidades;

namespace PlataformaReservas.Aplicacion.Abstracciones;

public interface IRepositorioReservas
{
    Task<Reserva?> ObtenerPorIdAsync(Guid id, CancellationToken ct);

    Task<Reserva?> ObtenerParaModificarAsync(Guid id, CancellationToken ct);
}
