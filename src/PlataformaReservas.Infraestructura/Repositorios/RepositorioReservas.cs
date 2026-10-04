using Microsoft.EntityFrameworkCore;
using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Infraestructura.Persistencia;

namespace PlataformaReservas.Infraestructura.Repositorios;

public sealed class RepositorioReservas(ContextoDatos db, IContextoEmpresa contexto) : IRepositorioReservas
{
    public Task<Reserva?> ObtenerPorIdAsync(Guid id, CancellationToken ct)
    {
        Guid empresaId = contexto.EmpresaActualId;
        return db.Reservas.FirstOrDefaultAsync(r => r.Id == id && r.EmpresaId == empresaId, ct);
    }

    public Task<Reserva?> ObtenerParaModificarAsync(Guid id, CancellationToken ct)
    {
        Guid empresaId = contexto.EmpresaActualId;
        return db.Reservas.AsTracking().FirstOrDefaultAsync(r => r.Id == id && r.EmpresaId == empresaId, ct);
    }
}
