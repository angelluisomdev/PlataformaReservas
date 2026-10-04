using System.Collections.Concurrent;
using Microsoft.AspNetCore.Identity;
using PlataformaReservas.Infraestructura.Identidad;

namespace PlataformaReservas.PruebasIntegracion.Identidad;

public sealed class RemitenteCaptura : IEmailSender<Usuario>
{
    private readonly ConcurrentDictionary<string, string> _enlaces = new(StringComparer.OrdinalIgnoreCase);

    public string? EnlaceDe(string correo) => _enlaces.GetValueOrDefault(correo);

    public Task SendPasswordResetLinkAsync(Usuario user, string email, string resetLink)
    {
        _enlaces[email] = resetLink;
        return Task.CompletedTask;
    }

    public Task SendConfirmationLinkAsync(Usuario user, string email, string confirmationLink) =>
        throw new NotSupportedException();

    public Task SendPasswordResetCodeAsync(Usuario user, string email, string resetCode) =>
        throw new NotSupportedException();
}
