using Microsoft.EntityFrameworkCore.Storage;
using PlataformaReservas.Aplicacion.Abstracciones;

namespace PlataformaReservas.Infraestructura.Persistencia;

public sealed class UnidadTrabajo(ContextoDatos db) : IUnidadTrabajo
{
    public async Task<ITransaccion> IniciarTransaccionAsync(CancellationToken ct)
    {
        return new Transaccion(await db.Database.BeginTransactionAsync(ct));
    }

    public Task GuardarCambiosAsync(CancellationToken ct)
    {
        return db.SaveChangesAsync(ct);
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
