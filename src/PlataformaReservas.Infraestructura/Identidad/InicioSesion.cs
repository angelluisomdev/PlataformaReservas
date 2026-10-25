using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using PlataformaReservas.Infraestructura.Correo;

namespace PlataformaReservas.Infraestructura.Identidad;

public sealed partial class InicioSesion(SignInManager<Usuario> sesiones, ILogger<InicioSesion> registro)
{
    public async Task<ClaimsPrincipal?> IniciarAsync(string correo, string clave, bool recordar, string? ip)
    {
        SignInResult resultado = await sesiones.PasswordSignInAsync(correo, clave, recordar, lockoutOnFailure: true);
        if (resultado.Succeeded)
        {
            RegistrarAcierto(registro);
            Usuario usuario = await sesiones.UserManager.FindByNameAsync(correo)
                ?? throw new InvalidOperationException("El usuario que acaba de iniciar sesion no existe.");
            return await sesiones.CreateUserPrincipalAsync(usuario);
        }

        string enmascarado = EnmascaradoCorreo.Enmascarar(correo);
        if (resultado.IsLockedOut)
        {
            RegistrarBloqueo(registro, enmascarado, ip);
        }
        else
        {
            RegistrarFallo(registro, enmascarado, ip);
        }

        return null;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Inicio de sesion correcto")]
    private static partial void RegistrarAcierto(ILogger registro);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Inicio de sesion fallido para {Correo} desde {IP}")]
    private static partial void RegistrarFallo(ILogger registro, string correo, string? ip);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Inicio de sesion rechazado por cuenta bloqueada para {Correo} desde {IP}")]
    private static partial void RegistrarBloqueo(ILogger registro, string correo, string? ip);
}
