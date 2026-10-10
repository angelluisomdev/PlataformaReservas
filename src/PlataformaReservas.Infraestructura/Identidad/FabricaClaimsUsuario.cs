using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.Repositorios;

namespace PlataformaReservas.Infraestructura.Identidad;

public sealed class FabricaClaimsUsuario(
    UserManager<Usuario> userManager,
    RoleManager<IdentityRole<Guid>> roleManager,
    IOptions<IdentityOptions> opciones,
    IRepositorioMiembros miembros)
    : UserClaimsPrincipalFactory<Usuario, IdentityRole<Guid>>(userManager, roleManager, opciones)
{
    public const string ClaimEmpresa = "empresa_id";

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(Usuario user)
    {
        ClaimsIdentity identidad = await base.GenerateClaimsAsync(user);

        MiembroEmpresa? miembro = await miembros.ObtenerPorUsuarioAsync(user.Id, CancellationToken.None);

        if (miembro is not null)
        {
            identidad.AddClaim(new Claim(ClaimEmpresa, miembro.EmpresaId.ToString("D", CultureInfo.InvariantCulture)));
        }

        return identidad;
    }
}
