using Microsoft.EntityFrameworkCore;
using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.Repositorios;
using PlataformaReservas.Infraestructura.Persistencia;

namespace PlataformaReservas.Infraestructura.Repositorios;

public sealed class RepositorioServicios(ContextoDatos db, IContextoEmpresa contexto) : IRepositorioServicios
{
    public Task<Servicio?> ObtenerPorIdAsync(Guid id, CancellationToken ct)
    {
        Guid empresaId = contexto.EmpresaActualId;
        return db.Servicios.FirstOrDefaultAsync(s => s.Id == id && s.EmpresaId == empresaId, ct);
    }

    public Task<Servicio?> ObtenerParaModificarAsync(Guid id, CancellationToken ct)
    {
        Guid empresaId = contexto.EmpresaActualId;
        return db.Servicios.AsTracking().FirstOrDefaultAsync(s => s.Id == id && s.EmpresaId == empresaId, ct);
    }

    public async Task<IReadOnlyList<Servicio>> ListarAsync(CancellationToken ct)
    {
        Guid empresaId = contexto.EmpresaActualId;
        return await db.Servicios.Where(x => x.EmpresaId == empresaId).ToListAsync(ct);
    }

    public void Agregar(Servicio servicio)
    {
        if (servicio.EmpresaId != contexto.EmpresaActualId)
        {
            throw new InvalidOperationException("No se puede agregar un recurso de otra empresa.");
        }

        db.Servicios.Add(servicio);
    }
}
