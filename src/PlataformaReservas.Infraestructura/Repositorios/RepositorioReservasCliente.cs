using Microsoft.EntityFrameworkCore;
using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.Enumeraciones;
using PlataformaReservas.Dominio.Repositorios;
using PlataformaReservas.Infraestructura.Persistencia;

namespace PlataformaReservas.Infraestructura.Repositorios;

public sealed class RepositorioReservasCliente(ContextoDatos db, IContextoUsuario contexto) : IRepositorioReservasCliente
{
    public Task<Reserva?> ObtenerParaModificarAsync(Guid id, CancellationToken ct)
    {
        Guid usuarioId = contexto.UsuarioActualId;
        return db.Reservas.AsTracking().FirstOrDefaultAsync(r => r.Id == id && r.UsuarioId == usuarioId, ct);
    }

    public async Task<IReadOnlyList<Reserva>> ObtenerFuturasConfirmadasParaModificarAsync(DateTime ahoraUtc, CancellationToken ct)
    {
        Guid usuarioId = contexto.UsuarioActualId;
        return await db.Reservas
            .AsTracking()
            .Where(r => r.UsuarioId == usuarioId && r.Estado == EstadoReserva.Confirmada && r.Franja.InicioUtc > ahoraUtc)
            .ToListAsync(ct);
    }

    public void Agregar(Reserva reserva)
    {
        if (reserva.UsuarioId != contexto.UsuarioActualId)
        {
            throw new InvalidOperationException("No se puede agregar una reserva de otro usuario.");
        }

        db.Reservas.Add(reserva);
    }
}
