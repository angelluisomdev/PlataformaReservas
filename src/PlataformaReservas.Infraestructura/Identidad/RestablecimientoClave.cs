using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;

namespace PlataformaReservas.Infraestructura.Identidad;

public sealed class RestablecimientoClave(UserManager<Usuario> usuarios)
{
    public const string CodigoEnlaceNoValido = "EnlaceNoValido";

    // RN-158 (Fase 19, duda 5): una cuenta inexistente y un enlace invalido dan la misma respuesta.
    public async Task<IdentityResult> RestablecerAsync(string correo, string codigo, string clave)
    {
        Usuario? usuario = await usuarios.FindByEmailAsync(correo);
        if (usuario is null || !IntentarDecodificar(codigo, out string token)
            || !await usuarios.VerifyUserTokenAsync(
                usuario, usuarios.Options.Tokens.PasswordResetTokenProvider, UserManager<Usuario>.ResetPasswordTokenPurpose, token))
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = CodigoEnlaceNoValido,
                Description = "El enlace no es válido o ha caducado.",
            });
        }

        return await usuarios.ResetPasswordAsync(usuario, token, clave);
    }

    public static bool IntentarDecodificar(string? codigo, out string token)
    {
        token = "";
        if (string.IsNullOrEmpty(codigo))
        {
            return false;
        }

        try
        {
            token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(codigo));
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
