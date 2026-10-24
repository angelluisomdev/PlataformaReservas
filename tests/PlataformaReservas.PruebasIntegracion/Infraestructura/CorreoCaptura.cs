using System.Collections.Concurrent;
using PlataformaReservas.Aplicacion.Abstracciones;

namespace PlataformaReservas.PruebasIntegracion.Infraestructura;

public sealed class CorreoCaptura : IServicioEmail
{
    public ConcurrentQueue<MensajeCorreo> Enviados { get; } = new();

    public bool Fallar { get; set; }

    public Task EnviarAsync(MensajeCorreo mensaje, CancellationToken ct)
    {
        if (Fallar)
        {
            throw new InvalidOperationException("El servidor de correo no responde.");
        }

        Enviados.Enqueue(mensaje);
        return Task.CompletedTask;
    }
}
