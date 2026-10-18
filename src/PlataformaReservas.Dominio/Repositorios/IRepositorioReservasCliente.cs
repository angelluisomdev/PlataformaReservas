using PlataformaReservas.Dominio.Entidades;

namespace PlataformaReservas.Dominio.Repositorios;

public interface IRepositorioReservasCliente
{
    Task<Reserva?> ObtenerParaModificarAsync(Guid id, CancellationToken ct);

    Task<IReadOnlyList<Reserva>> ObtenerFuturasConfirmadasParaModificarAsync(DateTime ahoraUtc, CancellationToken ct);

    void Agregar(Reserva reserva);
}
