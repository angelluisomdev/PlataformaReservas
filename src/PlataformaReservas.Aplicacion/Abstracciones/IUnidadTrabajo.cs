namespace PlataformaReservas.Aplicacion.Abstracciones;

public interface IUnidadTrabajo
{
    Task<ITransaccion> IniciarTransaccionAsync(CancellationToken ct);

    Task GuardarCambiosAsync(CancellationToken ct);
}
