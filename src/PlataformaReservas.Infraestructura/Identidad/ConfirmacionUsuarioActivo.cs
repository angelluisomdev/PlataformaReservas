using Microsoft.AspNetCore.Identity;

namespace PlataformaReservas.Infraestructura.Identidad;

public sealed class ConfirmacionUsuarioActivo : IUserConfirmation<Usuario>
{
    public Task<bool> IsConfirmedAsync(UserManager<Usuario> manager, Usuario user)
    {
        return Task.FromResult(user.Activo);
    }
}
