using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using MimeKit.Text;
using PlataformaReservas.Aplicacion.Abstracciones;

namespace PlataformaReservas.Infraestructura.Correo;

public sealed class ServicioEmailSmtp(IConfiguration configuracion) : IServicioEmail
{
    public async Task EnviarAsync(MensajeCorreo mensaje, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(mensaje);
        IConfigurationSection correo = configuracion.GetSection("Correo");

        try
        {
            using MimeMessage mime = new();
            mime.From.Add(new MailboxAddress(correo["NombreRemitente"], correo["Remitente"] ?? ""));
            mime.To.Add(MailboxAddress.Parse(mensaje.Para));
            mime.Subject = mensaje.Asunto;
            mime.Body = new TextPart(TextFormat.Html) { Text = mensaje.CuerpoHtml };

            using SmtpClient cliente = new();
            await cliente.ConnectAsync(correo["Servidor"] ?? "", correo.GetValue("Puerto", 587), SecureSocketOptions.StartTlsWhenAvailable, ct);
            string usuario = correo["Usuario"] ?? "";
            if (usuario.Length > 0)
            {
                await cliente.AuthenticateAsync(usuario, correo["Contrasena"] ?? "", ct);
            }

            await cliente.SendAsync(mime, ct);
            await cliente.DisconnectAsync(true, ct);
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            // Sin la excepcion original: MailKit puede incluir la direccion de destino en el mensaje (RN-172).
            throw new CorreoNoEnviadoException(error.GetType().Name);
        }
    }
}
