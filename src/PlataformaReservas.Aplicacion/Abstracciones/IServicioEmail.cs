namespace PlataformaReservas.Aplicacion.Abstracciones;

public interface IServicioEmail
{
    Task EnviarAsync(MensajeCorreo mensaje, CancellationToken ct);
}
