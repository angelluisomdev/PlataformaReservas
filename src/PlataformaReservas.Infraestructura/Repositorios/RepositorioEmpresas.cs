using Microsoft.EntityFrameworkCore;
using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.Repositorios;
using PlataformaReservas.Dominio.ValueObjects;
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

    // El slug es unico en toda la plataforma (RN-10): se comprueba sin filtrar por empresa y solo devuelve si existe.
    public Task<bool> ExisteSlugAsync(Slug slug, CancellationToken ct)
    {
        return db.Empresas.AnyAsync(e => e.Slug == slug, ct);
    }

    public void Agregar(Empresa empresa)
    {
        db.Empresas.Add(empresa);
    }
}
