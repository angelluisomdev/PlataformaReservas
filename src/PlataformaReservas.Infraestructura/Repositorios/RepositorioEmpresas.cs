using Microsoft.EntityFrameworkCore;
using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Infraestructura.Persistencia;

namespace PlataformaReservas.Infraestructura.Repositorios;

public sealed class RepositorioEmpresas(ContextoDatos db, IContextoEmpresa contexto) : IRepositorioEmpresas
{
    public Task<Empresa?> ObtenerPorIdAsync(Guid id, CancellationToken ct)
    {
        Guid empresaId = contexto.EmpresaActualId;
        return db.Empresas.FirstOrDefaultAsync(e => e.Id == id && e.Id == empresaId, ct);
    }

    public Task<Empresa?> ObtenerParaModificarAsync(Guid id, CancellationToken ct)
    {
        Guid empresaId = contexto.EmpresaActualId;
        return db.Empresas.AsTracking().FirstOrDefaultAsync(e => e.Id == id && e.Id == empresaId, ct);
    }
}
