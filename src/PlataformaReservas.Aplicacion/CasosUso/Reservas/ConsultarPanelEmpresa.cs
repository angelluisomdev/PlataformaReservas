using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Aplicacion.Disponibilidad;
using PlataformaReservas.Aplicacion.Dtos.Reservas;

namespace PlataformaReservas.Aplicacion.CasosUso.Reservas;

public sealed class ConsultarPanelEmpresa(IConsultasReservasEmpresa consultas, IRelojSistema reloj)
{
    public const int DiasProximas = 7;

    public async Task<Resultado<PanelEmpresaDto>> EjecutarAsync(CancellationToken ct)
    {
        DateTime ahora = reloj.AhoraUtc;
        DateOnly hoy = DateOnly.FromDateTime(MotorDisponibilidad.ALocal(ahora));
        (DateTime hoyDesde, DateTime hoyHasta) = MotorDisponibilidad.DiaLocalEnUtc(hoy);
        DateTime proximasHasta = MotorDisponibilidad.DiaLocalEnUtc(hoy.AddDays(DiasProximas)).HastaUtc;

        PanelEmpresaDto panel = await consultas.ObtenerPanelAsync(hoyDesde, hoyHasta, proximasHasta, ahora, ct);
        return Resultado<PanelEmpresaDto>.Exito(panel);
    }
}
