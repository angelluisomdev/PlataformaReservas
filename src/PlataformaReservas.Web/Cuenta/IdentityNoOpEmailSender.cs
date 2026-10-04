using Microsoft.AspNetCore.Identity;
using PlataformaReservas.Infraestructura.Identidad;

namespace PlataformaReservas.Web.Cuenta;

internal sealed class IdentityNoOpEmailSender : IEmailSender<Usuario>
{
    public Task SendConfirmationLinkAsync(Usuario user, string email, string confirmationLink) => Task.CompletedTask;

    public Task SendPasswordResetLinkAsync(Usuario user, string email, string resetLink) => Task.CompletedTask;

    public Task SendPasswordResetCodeAsync(Usuario user, string email, string resetCode) => Task.CompletedTask;
}
