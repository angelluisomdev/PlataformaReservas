using PlataformaReservas.Aplicacion.Abstracciones;

namespace PlataformaReservas.PruebasUnitarias.Infraestructura;

public sealed class CorreoCaptura : IServicioEmail
{
    public List<MensajeCorreo> Enviados { get; } = [];

    public Task EnviarAsync(MensajeCorreo mensaje, CancellationToken ct)
    {
        Enviados.Add(mensaje);
        return Task.CompletedTask;
    }
}
