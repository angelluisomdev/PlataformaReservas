using PlataformaReservas.Aplicacion.Dtos.Reservas;
using PlataformaReservas.Dominio.Enumeraciones;

namespace PlataformaReservas.Aplicacion.Abstracciones;

public interface IConsultasReservasEmpresa
{
    Task<IReadOnlyList<ReservaEmpresaDto>> ObtenerReservasAsync(
        DateTime desdeUtc, DateTime hastaUtc, Guid? profesionalId, EstadoReserva? estado, DateTime ahoraUtc, CancellationToken ct);

    Task<PanelEmpresaDto> ObtenerPanelAsync(
        DateTime hoyDesdeUtc, DateTime hoyHastaUtc, DateTime proximasHastaUtc, DateTime ahoraUtc, CancellationToken ct);
}
