using Microsoft.EntityFrameworkCore;
using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Dtos.Reservas;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.Enumeraciones;
using PlataformaReservas.Dominio.ValueObjects;
using PlataformaReservas.Infraestructura.Persistencia;

namespace PlataformaReservas.Infraestructura.Consultas;

public sealed class ConsultasReservasEmpresa(ContextoDatos db, IContextoEmpresa contexto) : IConsultasReservasEmpresa
{
    public async Task<IReadOnlyList<ReservaEmpresaDto>> ObtenerReservasAsync(
        DateTime desdeUtc, DateTime hastaUtc, Guid? profesionalId, EstadoReserva? estado, DateTime ahoraUtc, CancellationToken ct)
    {
        List<FilaReserva> filas = await Propias(desdeUtc, hastaUtc, profesionalId, estado).ToListAsync(ct);
        return filas.Select(f => ADto(f, ahoraUtc)).ToList();
    }

    public async Task<PanelEmpresaDto> ObtenerPanelAsync(
        DateTime hoyDesdeUtc, DateTime hoyHastaUtc, DateTime proximasHastaUtc, DateTime ahoraUtc, CancellationToken ct)
    {
        Guid empresaId = contexto.EmpresaActualId;
        bool activa = await db.Empresas.Where(e => e.Id == empresaId).Select(e => e.Activa).SingleAsync(ct);
        CondicionPublicable.Requisitos requisitos = await CondicionPublicable.RequisitosAsync(db, empresaId, ct);

        List<FilaReserva> hoy = await Propias(hoyDesdeUtc, hoyHastaUtc, null, null).ToListAsync(ct);
        List<FilaReserva> proximas = await Propias(hoyHastaUtc, proximasHastaUtc, null, EstadoReserva.Confirmada).ToListAsync(ct);

        return PanelEmpresaDto.ConvertirADto(
            activa, requisitos.TieneServicioActivo, requisitos.TieneProfesionalActivo, requisitos.TieneAsociacion, requisitos.EsPublicable,
            hoy.Select(f => ADto(f, ahoraUtc)), proximas.Select(f => ADto(f, ahoraUtc)));
    }

    private IQueryable<FilaReserva> Propias(DateTime desdeUtc, DateTime hastaUtc, Guid? profesionalId, EstadoReserva? estado)
    {
        Guid empresaId = contexto.EmpresaActualId;
        return from r in db.Reservas.AsNoTracking()
               where r.EmpresaId == empresaId
                     && r.Franja.InicioUtc >= desdeUtc && r.Franja.InicioUtc < hastaUtc
                     && (profesionalId == null || r.ProfesionalId == profesionalId)
                     && (estado == null || r.Estado == estado)
               join s in db.Servicios on r.ServicioId equals s.Id
               join p in db.Profesionales on r.ProfesionalId equals p.Id
               orderby r.Franja.InicioUtc
               select new FilaReserva(
                   r.Id, r.Franja.InicioUtc, r.Franja.FinUtc, s.Nombre, p.NombreCompleto, r.NombreCliente, r.TelefonoCliente,
                   r.Observaciones, r.Estado);
    }

    private static ReservaEmpresaDto ADto(FilaReserva fila, DateTime ahoraUtc)
    {
        return ReservaEmpresaDto.ConvertirADto(
            fila.Id, new FranjaHoraria(fila.InicioUtc, fila.FinUtc), fila.Servicio.Valor, fila.Profesional.Valor,
            fila.NombreCliente, fila.TelefonoCliente, fila.Observaciones, fila.Estado, ahoraUtc);
    }

    private sealed record FilaReserva(
        Guid Id,
        DateTime InicioUtc,
        DateTime FinUtc,
        NombreServicio Servicio,
        NombrePersona Profesional,
        NombrePersona NombreCliente,
        Telefono TelefonoCliente,
        Observaciones? Observaciones,
        EstadoReserva Estado);
}
