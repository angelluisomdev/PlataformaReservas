using Microsoft.EntityFrameworkCore;
using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Dtos.Empresas;
using PlataformaReservas.Aplicacion.Dtos.Profesionales;
using PlataformaReservas.Aplicacion.Dtos.Reservas;
using PlataformaReservas.Aplicacion.Dtos.Servicios;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.Enumeraciones;
using PlataformaReservas.Dominio.ValueObjects;
using PlataformaReservas.Infraestructura.Persistencia;

namespace PlataformaReservas.Infraestructura.Consultas;

public sealed class ConsultasDisponibilidad(ContextoDatos db) : IConsultasDisponibilidad
{
    public async Task<EmpresaDisponibilidadDto?> ObtenerEmpresaAsync(Guid empresaId, CancellationToken ct)
    {
        Empresa? empresa = await db.Empresas.FirstOrDefaultAsync(e => e.Id == empresaId, ct);
        return empresa is null ? null : EmpresaDisponibilidadDto.ConvertirADto(empresa);
    }

    public async Task<ServicioDisponibilidadDto?> ObtenerServicioAsync(Guid empresaId, Guid servicioId, CancellationToken ct)
    {
        Servicio? servicio = await db.Servicios.FirstOrDefaultAsync(s => s.Id == servicioId && s.EmpresaId == empresaId, ct);
        return servicio is null ? null : ServicioDisponibilidadDto.ConvertirADto(servicio);
    }

    public async Task<IReadOnlyList<ProfesionalDisponibilidadDto>> ObtenerProfesionalesQuePrestanAsync(
        Guid empresaId, Guid servicioId, Guid? profesionalId, CancellationToken ct)
    {
        List<Profesional> profesionales = await db.Profesionales
            .Include(p => p.Horarios)
            .Include(p => p.Excepciones)
            .Where(p => p.EmpresaId == empresaId && p.Activo)
            .Where(p => EF.Property<List<ProfesionalServicio>>(p, "_servicios").Any(s => s.ServicioId == servicioId))
            .Where(p => profesionalId == null || p.Id == profesionalId)
            .ToListAsync(ct);
        return profesionales.Select(ProfesionalDisponibilidadDto.ConvertirADto).ToList();
    }

    public async Task<IReadOnlyList<ReservaOcupadaDto>> ObtenerReservasActivasAsync(
        Guid empresaId, IReadOnlyCollection<Guid> profesionalIds, DateTime desdeUtc, DateTime hastaUtc, CancellationToken ct)
    {
        return await db.Reservas
            .Where(r => r.EmpresaId == empresaId && r.Estado != EstadoReserva.Cancelada)
            .Where(r => profesionalIds.Contains(r.ProfesionalId))
            .Where(r => r.Franja.InicioUtc < hastaUtc && r.Franja.FinUtc > desdeUtc)
            .Select(r => ReservaOcupadaDto.ConvertirADto(r.ProfesionalId, new FranjaHoraria(r.Franja.InicioUtc, r.Franja.FinUtc)))
            .ToListAsync(ct);
    }
}
