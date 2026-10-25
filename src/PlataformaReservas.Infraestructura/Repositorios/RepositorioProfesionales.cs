using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.Repositorios;
using PlataformaReservas.Infraestructura.Persistencia;

namespace PlataformaReservas.Infraestructura.Repositorios;

public sealed class RepositorioProfesionales(
    ContextoDatos db, IContextoEmpresa contexto, ILogger<RepositorioProfesionales> registro) : IRepositorioProfesionales
{
    public Task<Profesional?> ObtenerPorIdAsync(Guid id, CancellationToken ct)
    {
        Guid empresaId = contexto.EmpresaActualId;
        return db.Profesionales.FirstOrDefaultAsync(p => p.Id == id && p.EmpresaId == empresaId, ct);
    }

    public Task<Profesional?> ObtenerParaModificarAsync(Guid id, CancellationToken ct)
    {
        Guid empresaId = contexto.EmpresaActualId;
        return db.Profesionales
            .AsTracking()
            .Include(p => p.Horarios)
            .Include(p => p.Excepciones)
            .FirstOrDefaultAsync(p => p.Id == id && p.EmpresaId == empresaId, ct);
    }

    public Task<Profesional?> ObtenerConHorarioAsync(Guid id, CancellationToken ct)
    {
        Guid empresaId = contexto.EmpresaActualId;
        return db.Profesionales
            .Include(p => p.Horarios)
            .Include(p => p.Excepciones)
            .FirstOrDefaultAsync(p => p.Id == id && p.EmpresaId == empresaId, ct);
    }

    public async Task<IReadOnlyList<Profesional>> ListarAsync(CancellationToken ct)
    {
        Guid empresaId = contexto.EmpresaActualId;
        return await db.Profesionales.Where(x => x.EmpresaId == empresaId).ToListAsync(ct);
    }

    public void Agregar(Profesional profesional)
    {
        if (profesional.EmpresaId != contexto.EmpresaActualId)
        {
            EventosSeguridad.AccesoEntreEmpresas(registro, contexto.EmpresaActualId, profesional.EmpresaId);
            throw new InvalidOperationException("No se puede agregar un recurso de otra empresa.");
        }

        db.Profesionales.Add(profesional);
    }
}
