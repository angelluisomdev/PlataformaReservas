using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Infraestructura.Persistencia;

namespace PlataformaReservas.Infraestructura.Consultas;

public static class CondicionPublicable
{
    public static Expression<Func<Empresa, bool>> Aplica(ContextoDatos db)
    {
        return e => db.Servicios.Any(s =>
            s.EmpresaId == e.Id && s.Activo && db.Profesionales.Any(p =>
                p.EmpresaId == e.Id && p.Activo && p.Horarios.Any()
                && EF.Property<List<ProfesionalServicio>>(p, "_servicios").Any(ps => ps.ServicioId == s.Id)));
    }

    public static async Task<Requisitos> RequisitosAsync(ContextoDatos db, Guid empresaId, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(db);
        bool tieneServicioActivo = await db.Servicios.AnyAsync(s => s.EmpresaId == empresaId && s.Activo, ct);
        bool tieneProfesionalActivo = await db.Profesionales.AnyAsync(p => p.EmpresaId == empresaId && p.Activo, ct);
        bool tieneAsociacion = await db.Profesionales.AnyAsync(p =>
            p.EmpresaId == empresaId && p.Activo
            && EF.Property<List<ProfesionalServicio>>(p, "_servicios").Any(ps =>
                db.Servicios.Any(s => s.Id == ps.ServicioId && s.EmpresaId == empresaId && s.Activo)), ct);
        bool esPublicable = await db.Empresas.Where(e => e.Id == empresaId).AnyAsync(Aplica(db), ct);
        return new Requisitos(tieneServicioActivo, tieneProfesionalActivo, tieneAsociacion, esPublicable);
    }

    public sealed record Requisitos(bool TieneServicioActivo, bool TieneProfesionalActivo, bool TieneAsociacion, bool EsPublicable);
}
