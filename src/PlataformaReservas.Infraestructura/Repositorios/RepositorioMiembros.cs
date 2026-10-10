using Microsoft.EntityFrameworkCore;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.Repositorios;
using PlataformaReservas.Infraestructura.Persistencia;

namespace PlataformaReservas.Infraestructura.Repositorios;

public sealed class RepositorioMiembros(ContextoDatos db) : IRepositorioMiembros
{
    // Excepcion deliberada al filtro por empresa: esta consulta es la que resuelve la empresa al iniciar sesion.
    public Task<MiembroEmpresa?> ObtenerPorUsuarioAsync(Guid usuarioId, CancellationToken ct)
    {
        return db.MiembrosEmpresa.FirstOrDefaultAsync(m => m.UsuarioId == usuarioId, ct);
    }

    public void Agregar(MiembroEmpresa miembro)
    {
        db.MiembrosEmpresa.Add(miembro);
    }
}
