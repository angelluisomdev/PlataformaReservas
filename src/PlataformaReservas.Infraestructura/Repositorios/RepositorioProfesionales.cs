using Microsoft.EntityFrameworkCore;
using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Infraestructura.Persistencia;

namespace PlataformaReservas.Infraestructura.Repositorios;

public sealed class RepositorioProfesionales(ContextoDatos db, IContextoEmpresa contexto) : IRepositorioProfesionales
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
}
