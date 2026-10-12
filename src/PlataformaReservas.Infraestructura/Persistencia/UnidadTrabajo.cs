using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Infraestructura.Errores;

namespace PlataformaReservas.Infraestructura.Persistencia;

public sealed class UnidadTrabajo(ContextoDatos db) : IUnidadTrabajo
{
    public async Task<ITransaccion> IniciarTransaccionAsync(CancellationToken ct)
    {
        return new Transaccion(await db.Database.BeginTransactionAsync(ct));
    }

    public async Task GuardarCambiosAsync(CancellationToken ct)
    {
        bool insertaReservas = db.ChangeTracker.Entries<Reserva>().Any(e => e.State == EntityState.Added);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (Exception error) when (TraductorErroresPostgres.EsHuecoOcupado(error, insertaReservas))
        {
            DesengancharReservasRechazadas();
            throw new HuecoOcupadoException(error);
        }
    }

    // El ContextoDatos del circuito sobrevive al fallo: sin esto, el siguiente guardado reinsertaria la reserva rechazada.
    private void DesengancharReservasRechazadas()
    {
        foreach (EntityEntry<Reserva> entrada in db.ChangeTracker.Entries<Reserva>().Where(e => e.State == EntityState.Added).ToList())
        {
            entrada.State = EntityState.Detached;
        }
    }

    private sealed class Transaccion(IDbContextTransaction transaccion) : ITransaccion
    {
        public Task ConfirmarAsync(CancellationToken ct)
        {
            return transaccion.CommitAsync(ct);
        }

        public ValueTask DisposeAsync()
        {
            return transaccion.DisposeAsync();
        }
    }
}
