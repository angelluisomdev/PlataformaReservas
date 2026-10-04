using Microsoft.EntityFrameworkCore;
using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Dominio.Entidades;
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
}
