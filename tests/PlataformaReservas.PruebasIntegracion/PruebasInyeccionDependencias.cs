using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Dominio.Repositorios;
using PlataformaReservas.Infraestructura;
using PlataformaReservas.Infraestructura.Identidad;
using PlataformaReservas.PruebasIntegracion.Infraestructura;

namespace PlataformaReservas.PruebasIntegracion;

[Collection("BaseDatos")]
public sealed class PruebasInyeccionDependencias(BaseDatosFixture baseDatos)
{
    [Fact]
    public async Task EsPropietario_exige_el_rol_y_el_claim_de_empresa()
    {
        ServiceProvider servicios = baseDatos.ServiciosIdentidad;
        IAuthorizationService autorizacion = servicios.GetRequiredService<IAuthorizationService>();

        ClaimsPrincipal sinEmpresa = Principal(new Claim(ClaimTypes.Role, RolesIdentidad.Propietario));
        ClaimsPrincipal conEmpresa = Principal(
            new Claim(ClaimTypes.Role, RolesIdentidad.Propietario),
            new Claim(FabricaClaimsUsuario.ClaimEmpresa, Guid.CreateVersion7().ToString()));
        ClaimsPrincipal cliente = Principal(new Claim(ClaimTypes.Role, RolesIdentidad.Cliente));

        (await autorizacion.AuthorizeAsync(sinEmpresa, RolesIdentidad.PoliticaPropietario)).Succeeded.Should().BeFalse();
        (await autorizacion.AuthorizeAsync(conEmpresa, RolesIdentidad.PoliticaPropietario)).Succeeded.Should().BeTrue();
        (await autorizacion.AuthorizeAsync(cliente, RolesIdentidad.PoliticaCliente)).Succeeded.Should().BeTrue();
        (await autorizacion.AuthorizeAsync(cliente, RolesIdentidad.PoliticaPropietario)).Succeeded.Should().BeFalse();
    }

    [Fact]
    public void La_cookie_revalida_el_sello_cada_cinco_minutos()
    {
        SecurityStampValidatorOptions opciones = baseDatos.ServiciosIdentidad
            .GetRequiredService<IOptions<SecurityStampValidatorOptions>>().Value;

        opciones.ValidationInterval.Should().Be(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void Sin_claves_configuradas_la_comprobacion_de_arranque_falla()
    {
        IConfiguration configuracion = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PlataformaReservas"] = "Host=localhost",
            })
            .Build();
        ServiceCollection servicios = new();
        servicios.AddLogging();
        servicios.AgregarInfraestructura(configuracion);
        using ServiceProvider proveedor = servicios.BuildServiceProvider();

        Action comprobar = proveedor.ComprobarClavesCifrado;

        comprobar.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void El_contexto_de_empresa_y_los_repositorios_son_scoped()
    {
        Type[] tipos =
        [
            typeof(IContextoEmpresa), typeof(IRepositorioEmpresas), typeof(IRepositorioServicios),
            typeof(IRepositorioProfesionales), typeof(IRepositorioReservas), typeof(IRepositorioMiembros),
        ];
        ServiceCollection servicios = new();
        servicios.AgregarInfraestructura(new ConfigurationBuilder().Build());

        servicios.Where(d => tipos.Contains(d.ServiceType))
            .Should().HaveCount(tipos.Length)
            .And.OnlyContain(d => d.Lifetime == ServiceLifetime.Scoped);
    }

    private static ClaimsPrincipal Principal(params Claim[] claims)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Prueba"));
    }
}
