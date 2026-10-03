using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using PlataformaReservas.Infraestructura.Identidad;

namespace PlataformaReservas.Web.Cuenta;

internal sealed class IdentityNoOpEmailSender : IEmailSender<Usuario>
{
    private readonly IEmailSender emailSender = new NoOpEmailSender();

    public Task SendConfirmationLinkAsync(Usuario user, string email, string confirmationLink) =>
        emailSender.SendEmailAsync(email, "Confirma tu correo", $"Confirma tu cuenta <a href='{confirmationLink}'>aquí</a>.");

    public Task SendPasswordResetLinkAsync(Usuario user, string email, string resetLink) =>
        emailSender.SendEmailAsync(email, "Restablece tu contraseña", $"Restablece tu contraseña <a href='{resetLink}'>aquí</a>.");

    public Task SendPasswordResetCodeAsync(Usuario user, string email, string resetCode) =>
        emailSender.SendEmailAsync(email, "Restablece tu contraseña", $"Restablece tu contraseña con este código: {resetCode}");
}
