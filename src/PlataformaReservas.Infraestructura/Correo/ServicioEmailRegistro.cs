using Microsoft.Extensions.Logging;
using PlataformaReservas.Aplicacion.Abstracciones;

namespace PlataformaReservas.Infraestructura.Correo;

public sealed partial class ServicioEmailRegistro(ILogger<ServicioEmailRegistro> registro) : IServicioEmail
{
    public Task EnviarAsync(MensajeCorreo mensaje, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(mensaje);
        string destinatario = EnmascaradoCorreo.Enmascarar(mensaje.Para);
        RegistrarCorreo(registro, destinatario, mensaje.Asunto);
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Correo no enviado (envio deshabilitado) a {Destinatario}: {Asunto}")]
    private static partial void RegistrarCorreo(ILogger registro, string destinatario, string asunto);
}
