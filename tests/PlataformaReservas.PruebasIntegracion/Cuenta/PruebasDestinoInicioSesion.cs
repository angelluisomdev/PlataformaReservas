using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using PlataformaReservas.Infraestructura.Identidad;
using PlataformaReservas.PruebasIntegracion.Infraestructura;
using PlataformaReservas.Web.Cuenta;

namespace PlataformaReservas.PruebasIntegracion.Cuenta;

[Collection("BaseDatos")]
public sealed class PruebasDestinoInicioSesion(BaseDatosFixture baseDatos)
{
    [Fact]
    public async Task Un_propietario_va_al_area_de_empresa()
    {
        (await CalcularAsync(Usuario(RolesIdentidad.Propietario, conEmpresa: true))).Should().Be("app");
    }

    [Fact]
    public async Task Un_cliente_va_a_sus_reservas()
    {
        (await CalcularAsync(Usuario(RolesIdentidad.Cliente))).Should().Be("my-account/bookings");
    }

    [Fact]
    public async Task Con_los_dos_roles_gana_el_propietario()
    {
        (await CalcularAsync(Usuario(RolesIdentidad.Cliente, RolesIdentidad.Propietario, conEmpresa: true))).Should().Be("app");
    }

    [Fact]
    public async Task Un_propietario_sin_empresa_no_va_al_area_de_empresa()
    {
        (await CalcularAsync(Usuario(RolesIdentidad.Cliente, RolesIdentidad.Propietario))).Should().Be("my-account/bookings");
    }

    [Fact]
    public async Task Sin_roles_va_a_la_portada()
    {
        (await CalcularAsync(Usuario())).Should().BeEmpty();
    }

    private static ClaimsPrincipal Usuario(params string[] roles) => Usuario(roles, conEmpresa: false);

    private static ClaimsPrincipal Usuario(string rol, bool conEmpresa) => Usuario([rol], conEmpresa);

    private static ClaimsPrincipal Usuario(string rol, string otroRol, bool conEmpresa) => Usuario([rol, otroRol], conEmpresa);

    private static ClaimsPrincipal Usuario(string[] roles, bool conEmpresa)
    {
        List<Claim> claims = [new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())];
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        if (conEmpresa)
        {
            claims.Add(new Claim(FabricaClaimsUsuario.ClaimEmpresa, Guid.NewGuid().ToString()));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Prueba"));
    }

    private async Task<string> CalcularAsync(ClaimsPrincipal usuario)
    {
        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        DestinoInicioSesion destino = new(ambito.ServiceProvider.GetRequiredService<IAuthorizationService>());
        return await destino.CalcularAsync(usuario);
    }
}
