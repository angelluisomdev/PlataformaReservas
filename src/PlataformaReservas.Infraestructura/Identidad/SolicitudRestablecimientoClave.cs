using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;

namespace PlataformaReservas.Infraestructura.Identidad;

public sealed class SolicitudRestablecimientoClave(
    UserManager<Usuario> usuarios,
    IUserConfirmation<Usuario> confirmacion,
    IEmailSender<Usuario> remitente)
{
    public async Task SolicitarAsync(string correo, Func<string, string> construirEnlace)
    {
        Usuario? usuario = await usuarios.FindByEmailAsync(correo);
        if (usuario is null || !await confirmacion.IsConfirmedAsync(usuarios, usuario))
        {
            return;
        }

        string token = await usuarios.GeneratePasswordResetTokenAsync(usuario);
        string codigo = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

        await remitente.SendPasswordResetLinkAsync(usuario, correo, construirEnlace(codigo));
    }
}
