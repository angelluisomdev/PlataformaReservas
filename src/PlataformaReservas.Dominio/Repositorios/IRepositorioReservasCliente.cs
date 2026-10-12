using PlataformaReservas.Dominio.Entidades;

namespace PlataformaReservas.Dominio.Repositorios;

public interface IRepositorioReservasCliente
{
    Task<Reserva?> ObtenerParaModificarAsync(Guid id, CancellationToken ct);

    void Agregar(Reserva reserva);
}
