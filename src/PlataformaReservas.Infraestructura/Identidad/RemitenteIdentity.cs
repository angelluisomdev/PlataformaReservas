using Microsoft.AspNetCore.Identity;
using PlataformaReservas.Aplicacion.Abstracciones;

namespace PlataformaReservas.Infraestructura.Identidad;

public sealed class RemitenteIdentity(IServicioEmail correo, IRenderizadorPlantillas plantillas) : IEmailSender<Usuario>
{
    public async Task SendPasswordResetLinkAsync(Usuario user, string email, string resetLink)
    {
        ArgumentNullException.ThrowIfNull(user);
        MensajeCorreo mensaje = await plantillas.RestablecimientoClaveAsync(email, user.NombreCompleto, resetLink);
        await correo.EnviarAsync(mensaje, CancellationToken.None);
    }

    public Task SendConfirmationLinkAsync(Usuario user, string email, string confirmationLink)
    {
        throw new NotSupportedException("La confirmacion del correo no esta en el MVP.");
    }

    public Task SendPasswordResetCodeAsync(Usuario user, string email, string resetCode)
    {
        throw new NotSupportedException("El restablecimiento usa enlace, no codigo.");
    }
}
