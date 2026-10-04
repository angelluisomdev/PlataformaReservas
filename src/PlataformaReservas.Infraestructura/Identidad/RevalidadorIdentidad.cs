using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PlataformaReservas.Infraestructura.Identidad;

public sealed class RevalidadorIdentidad(
    ILoggerFactory loggerFactory,
    IServiceScopeFactory fabricaAmbitos,
    IOptions<IdentityOptions> opciones)
    : RevalidatingServerAuthenticationStateProvider(loggerFactory)
{
    internal static readonly TimeSpan IntervaloRevalidacion = TimeSpan.FromMinutes(5);

    protected override TimeSpan RevalidationInterval => IntervaloRevalidacion;

    protected override async Task<bool> ValidateAuthenticationStateAsync(
        AuthenticationState authenticationState, CancellationToken cancellationToken)
    {
        await using AsyncServiceScope ambito = fabricaAmbitos.CreateAsyncScope();
        UserManager<Usuario> usuarios = ambito.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
        return await SelloVigenteAsync(usuarios, authenticationState.User);
    }

    private async Task<bool> SelloVigenteAsync(UserManager<Usuario> usuarios, ClaimsPrincipal principal)
    {
        Usuario? usuario = await usuarios.GetUserAsync(principal);
        if (usuario is null)
        {
            return false;
        }

        if (!usuarios.SupportsUserSecurityStamp)
        {
            return true;
        }

        string? selloPrincipal = principal.FindFirstValue(opciones.Value.ClaimsIdentity.SecurityStampClaimType);
        string selloUsuario = await usuarios.GetSecurityStampAsync(usuario);
        return selloPrincipal == selloUsuario;
    }
}
