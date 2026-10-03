using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PlataformaReservas.Infraestructura.Persistencia;

namespace PlataformaReservas.Infraestructura.Identidad;

public sealed class FabricaClaimsUsuario(
    UserManager<Usuario> userManager,
    RoleManager<IdentityRole<Guid>> roleManager,
    IOptions<IdentityOptions> opciones,
    ContextoDatos contexto)
    : UserClaimsPrincipalFactory<Usuario, IdentityRole<Guid>>(userManager, roleManager, opciones)
{
    public const string ClaimEmpresa = "empresa_id";

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(Usuario user)
    {
        ClaimsIdentity identidad = await base.GenerateClaimsAsync(user);

        Guid? empresaId = await contexto.MiembrosEmpresa
            .Where(m => m.UsuarioId == user.Id)
            .Select(m => (Guid?)m.EmpresaId)
            .FirstOrDefaultAsync();

        if (empresaId is not null)
        {
            identidad.AddClaim(new Claim(ClaimEmpresa, empresaId.Value.ToString("D", CultureInfo.InvariantCulture)));
        }

        return identidad;
    }
}
