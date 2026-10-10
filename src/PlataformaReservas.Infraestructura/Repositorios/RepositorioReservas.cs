using Microsoft.EntityFrameworkCore;
using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.Enumeraciones;
using PlataformaReservas.Dominio.Repositorios;
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

    public async Task<IReadOnlyList<Reserva>> ObtenerFuturasConfirmadasParaModificarAsync(DateTime ahoraUtc, CancellationToken ct)
    {
        Guid empresaId = contexto.EmpresaActualId;
        return await db.Reservas
            .AsTracking()
            .Where(r => r.EmpresaId == empresaId && r.Estado == EstadoReserva.Confirmada && r.Franja.InicioUtc > ahoraUtc)
            .ToListAsync(ct);
    }

    public Task<int> ContarFuturasConfirmadasAsync(DateTime ahoraUtc, CancellationToken ct)
    {
        Guid empresaId = contexto.EmpresaActualId;
        return db.Reservas.CountAsync(
            r => r.EmpresaId == empresaId && r.Estado == EstadoReserva.Confirmada && r.Franja.InicioUtc > ahoraUtc, ct);
    }
}
