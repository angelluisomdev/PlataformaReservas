namespace PlataformaReservas.Aplicacion.Abstracciones;

public interface ITransaccion : IAsyncDisposable
{
    Task ConfirmarAsync(CancellationToken ct);
}
