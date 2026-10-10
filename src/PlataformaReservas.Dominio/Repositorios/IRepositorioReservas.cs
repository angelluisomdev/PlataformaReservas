using PlataformaReservas.Dominio.Entidades;

namespace PlataformaReservas.Dominio.Repositorios;

public interface IRepositorioReservas
{
    Task<Reserva?> ObtenerPorIdAsync(Guid id, CancellationToken ct);

    Task<Reserva?> ObtenerParaModificarAsync(Guid id, CancellationToken ct);

    Task<IReadOnlyList<Reserva>> ObtenerFuturasConfirmadasParaModificarAsync(DateTime ahoraUtc, CancellationToken ct);

    Task<int> ContarFuturasConfirmadasAsync(DateTime ahoraUtc, CancellationToken ct);
}
