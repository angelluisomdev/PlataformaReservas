using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Infraestructura.Errores;

namespace PlataformaReservas.Infraestructura.Persistencia;

public sealed partial class UnidadTrabajo(ContextoDatos db, ILogger<UnidadTrabajo> registro) : IUnidadTrabajo
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
            RegistrarHuecoOcupado(registro);
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

    [LoggerMessage(Level = LogLevel.Warning, Message = "Reserva rechazada por la base de datos: el hueco ya estaba ocupado")]
    private static partial void RegistrarHuecoOcupado(ILogger registro);

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
