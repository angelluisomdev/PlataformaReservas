using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PlataformaReservas.Infraestructura.Identidad;
using PlataformaReservas.PruebasIntegracion.Infraestructura;
using static PlataformaReservas.PruebasIntegracion.Infraestructura.UsuariosPrueba;

namespace PlataformaReservas.PruebasIntegracion.Identidad;

[Collection("BaseDatos")]
public sealed class PruebasRevalidadorIdentidad(BaseDatosFixture baseDatos)
{
    [Fact]
    public async Task Un_principal_con_el_sello_vigente_sigue_valido()
    {
        Usuario usuario = await CrearAsync(baseDatos, CorreoUnico());
        ClaimsPrincipal principal = await PrincipalDeAsync(usuario);

        (await SelloVigenteAsync(principal)).Should().BeTrue();
    }

    [Fact]
    public async Task Un_principal_deja_de_ser_valido_cuando_se_renueva_el_sello()
    {
        Usuario usuario = await CrearAsync(baseDatos, CorreoUnico());
        ClaimsPrincipal principal = await PrincipalDeAsync(usuario);

        await using (AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope())
        {
            UserManager<Usuario> usuarios = ambito.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
            Usuario guardado = (await usuarios.FindByIdAsync(usuario.Id.ToString()))!;
            (await usuarios.UpdateSecurityStampAsync(guardado)).Succeeded.Should().BeTrue();
        }

        (await SelloVigenteAsync(principal)).Should().BeFalse();
    }

    [Fact]
    public async Task Un_principal_de_un_usuario_que_no_existe_no_es_valido()
    {
        ClaimsPrincipal principal = new(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, Guid.CreateVersion7().ToString())], "Prueba"));

        (await SelloVigenteAsync(principal)).Should().BeFalse();
    }

    private async Task<ClaimsPrincipal> PrincipalDeAsync(Usuario usuario)
    {
        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        UserManager<Usuario> usuarios = ambito.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
        IUserClaimsPrincipalFactory<Usuario> fabrica = ambito.ServiceProvider.GetRequiredService<IUserClaimsPrincipalFactory<Usuario>>();

        Usuario guardado = (await usuarios.FindByIdAsync(usuario.Id.ToString()))!;
        return await fabrica.CreateAsync(guardado);
    }

    private async Task<bool> SelloVigenteAsync(ClaimsPrincipal principal)
    {
        IServiceProvider servicios = baseDatos.ServiciosIdentidad;
        using RevalidadorIdentidad revalidador = new(
            servicios.GetRequiredService<ILoggerFactory>(),
            servicios.GetRequiredService<IServiceScopeFactory>(),
            servicios.GetRequiredService<IOptions<IdentityOptions>>());

        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        UserManager<Usuario> usuarios = ambito.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
        return await revalidador.SelloVigenteAsync(usuarios, principal);
    }
}
