using PlataformaReservas.Aplicacion.Dtos.Reservas;

namespace PlataformaReservas.Aplicacion.Abstracciones;

public interface IConsultasReservasCliente
{
    Task<MisReservasDto> ObtenerMisReservasAsync(DateTime ahoraUtc, CancellationToken ct);

    Task<DetalleReservaClienteDto?> ObtenerDetalleAsync(Guid reservaId, DateTime ahoraUtc, CancellationToken ct);
}
