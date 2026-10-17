using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Dtos.Empresas;
using PlataformaReservas.Aplicacion.Dtos.Profesionales;
using PlataformaReservas.Aplicacion.Dtos.Reservas;
using PlataformaReservas.Aplicacion.Dtos.Servicios;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.Aplicacion.Disponibilidad;

public sealed class MotorDisponibilidad(IConsultasDisponibilidad consultas, IRelojSistema reloj) : IMotorDisponibilidad
{
    private static readonly TimeZoneInfo ZonaHoraria = TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid");

    public async Task<IReadOnlyList<HuecoDisponible>> ObtenerHuecosAsync(
        Guid empresaId, Guid servicioId, Guid? profesionalId, DateOnly fecha, CancellationToken ct)
    {
        EmpresaDisponibilidadDto? empresa = await consultas.ObtenerEmpresaAsync(empresaId, ct);
        if (empresa is null || !empresa.Activa)
        {
            return [];
        }

        ServicioDisponibilidadDto? servicio = await consultas.ObtenerServicioAsync(empresaId, servicioId, ct);
        if (servicio is null || !servicio.Activo)
        {
            return [];
        }

        PoliticasReserva politicas = PoliticasReserva.Resolver(
            new PoliticasReserva(empresa.IntervaloHuecosMinutos, empresa.AntelacionMinimaMinutos, empresa.AntelacionMaximaDias),
            IntervaloHuecos.Desde(servicio.IntervaloHuecosMinutos),
            AntelacionMinimaReserva.Desde(servicio.AntelacionMinimaMinutos),
            AntelacionMaximaReserva.Desde(servicio.AntelacionMaximaDias));
        DateTime ahoraUtc = reloj.AhoraUtc;
        if (SuperaLaVentana(fecha, politicas.AntelacionMaximaDias, ahoraUtc))
        {
            return [];
        }

        IReadOnlyList<ProfesionalDisponibilidadDto> profesionales = await consultas.ObtenerProfesionalesQuePrestanAsync(
            empresaId, servicioId, profesionalId, ct);
        DateTime limiteAntelacion = ahoraUtc.AddMinutes(politicas.AntelacionMinimaMinutos);
        List<(ProfesionalDisponibilidadDto Profesional, FranjaHoraria Franja)> candidatos = profesionales
            .Where(p => !p.FechasNoDisponibles.Contains(fecha))
            .SelectMany(p => Candidatos(p, fecha, servicio.DuracionMinutos, politicas.IntervaloHuecosMinutos))
            .Where(c => c.Franja.InicioUtc >= limiteAntelacion)
            .ToList();
        if (candidatos.Count == 0)
        {
            return [];
        }

        IReadOnlyList<ReservaOcupadaDto> reservas = await consultas.ObtenerReservasActivasAsync(
            empresaId,
            candidatos.Select(c => c.Profesional.Id).Distinct().ToList(),
            candidatos.Min(c => c.Franja.InicioUtc),
            candidatos.Max(c => c.Franja.FinUtc),
            ct);

        return candidatos
            .Where(c => !reservas.Any(r => r.ProfesionalId == c.Profesional.Id && new FranjaHoraria(r.InicioUtc, r.FinUtc).SeSolapaCon(c.Franja)))
            .Select(c => new HuecoDisponible(c.Profesional.Id, c.Profesional.NombreCompleto, c.Franja.InicioUtc, c.Franja.FinUtc))
            .OrderBy(h => h.InicioUtc)
            .ThenBy(h => h.NombreProfesional, StringComparer.CurrentCulture)
            .ToList();
    }

    private static IEnumerable<(ProfesionalDisponibilidadDto Profesional, FranjaHoraria Franja)> Candidatos(
        ProfesionalDisponibilidadDto profesional, DateOnly fecha, int duracionMinutos, int intervaloMinutos)
    {
        TimeSpan minutos = TimeSpan.FromMinutes(duracionMinutos);
        TimeSpan paso = TimeSpan.FromMinutes(intervaloMinutos);

        foreach (IntervaloHorarioDto horario in profesional.Horarios.Where(h => h.DiaSemana == fecha.DayOfWeek))
        {
            TimeSpan fin = horario.HoraFin.ToTimeSpan();
            for (TimeSpan cursor = horario.HoraInicio.ToTimeSpan(); cursor + minutos <= fin; cursor += paso)
            {
                DateTime? inicioUtc = ALocalUtc(fecha, cursor);
                if (inicioUtc is null || ALocalUtc(fecha, cursor + minutos) is null)
                {
                    continue;
                }

                yield return (profesional, new FranjaHoraria(inicioUtc.Value, inicioUtc.Value + minutos));
            }
        }
    }

    public static (DateOnly Primera, DateOnly Ultima) FechasReservables(DateTime ahoraUtc, int antelacionMaximaDias)
    {
        DateOnly hoy = DateOnly.FromDateTime(ALocal(ahoraUtc));
        return (hoy, hoy.AddDays(antelacionMaximaDias));
    }

    public static DateTime ALocal(DateTime instanteUtc)
    {
        if (instanteUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("El instante debe estar en UTC.", nameof(instanteUtc));
        }

        return TimeZoneInfo.ConvertTimeFromUtc(instanteUtc, ZonaHoraria);
    }

    private static bool SuperaLaVentana(DateOnly fecha, int ventanaDias, DateTime ahoraUtc)
    {
        return fecha > FechasReservables(ahoraUtc, ventanaDias).Ultima;
    }

    // Unico punto de conversion local <-> UTC (RN-102), junto con ALocal. Una hora local inexistente no se ofrece (fase 10, duda 2).
    private static DateTime? ALocalUtc(DateOnly fecha, TimeSpan hora)
    {
        DateTime local = fecha.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified).Add(hora);
        if (ZonaHoraria.IsInvalidTime(local))
        {
            return null;
        }

        return TimeZoneInfo.ConvertTimeToUtc(local, ZonaHoraria);
    }
}
