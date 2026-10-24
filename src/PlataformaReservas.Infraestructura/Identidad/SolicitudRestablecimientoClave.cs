using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace PlataformaReservas.Infraestructura.Identidad;

public sealed partial class SolicitudRestablecimientoClave(
    UserManager<Usuario> usuarios,
    IUserConfirmation<Usuario> confirmacion,
    IEmailSender<Usuario> remitente,
    IConfiguration configuracion,
    ILogger<SolicitudRestablecimientoClave> registro)
{
    public const string RutaRestablecimiento = "Account/ResetPassword";

    // RN-158: nada de lo que ocurra aqui puede distinguir una cuenta existente de una inexistente.
    // Por eso el enlace no sale del Host de la peticion (manipulable) y un fallo de envio no se propaga.
    public async Task SolicitarAsync(string correo)
    {
        Usuario? usuario = await usuarios.FindByEmailAsync(correo);
        if (usuario is null || !await confirmacion.IsConfirmedAsync(usuarios, usuario))
        {
            return;
        }

        string urlBase = (configuracion["Correo:UrlBase"] ?? "").TrimEnd('/');
        if (!Uri.TryCreate(urlBase, UriKind.Absolute, out Uri? raiz) || (raiz.Scheme != Uri.UriSchemeHttps && raiz.Scheme != Uri.UriSchemeHttp))
        {
            RegistrarSinUrlBase(registro);
            return;
        }

        string token = await usuarios.GeneratePasswordResetTokenAsync(usuario);
        string codigo = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        string enlace = $"{urlBase}/{RutaRestablecimiento}?code={codigo}";

        try
        {
            await remitente.SendPasswordResetLinkAsync(usuario, correo, enlace);
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            RegistrarFalloEnvio(registro, error.GetType().Name);
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "No se pudo enviar el correo de restablecimiento ({TipoFallo})")]
    private static partial void RegistrarFalloEnvio(ILogger registro, string tipoFallo);

    [LoggerMessage(Level = LogLevel.Error, Message = "Correo:UrlBase no es una URL http(s) absoluta: no se envia el restablecimiento")]
    private static partial void RegistrarSinUrlBase(ILogger registro);
}
