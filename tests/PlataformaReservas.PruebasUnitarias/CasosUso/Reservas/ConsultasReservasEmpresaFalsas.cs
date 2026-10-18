using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Dtos.Reservas;
using PlataformaReservas.Dominio.Enumeraciones;

namespace PlataformaReservas.PruebasUnitarias.CasosUso.Reservas;

public sealed class ConsultasReservasEmpresaFalsas : IConsultasReservasEmpresa
{
    public List<(DateTime HoyDesde, DateTime HoyHasta, DateTime ProximasHasta)> Paneles { get; } = [];

    public Task<IReadOnlyList<ReservaEmpresaDto>> ObtenerReservasAsync(
        DateTime desdeUtc, DateTime hastaUtc, Guid? profesionalId, EstadoReserva? estado, DateTime ahoraUtc, CancellationToken ct)
    {
        return Task.FromResult<IReadOnlyList<ReservaEmpresaDto>>([]);
    }

    public Task<PanelEmpresaDto> ObtenerPanelAsync(
        DateTime hoyDesdeUtc, DateTime hoyHastaUtc, DateTime proximasHastaUtc, DateTime ahoraUtc, CancellationToken ct)
    {
        Paneles.Add((hoyDesdeUtc, hoyHastaUtc, proximasHastaUtc));
        return Task.FromResult(PanelEmpresaDto.ConvertirADto(true, true, true, true, true, [], []));
    }
}
