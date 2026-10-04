using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.Enumeraciones;
using PlataformaReservas.Dominio.ValueObjects;
using PlataformaReservas.Infraestructura.Identidad;
using PlataformaReservas.Infraestructura.Persistencia;
using PlataformaReservas.PruebasIntegracion.Infraestructura;
using static PlataformaReservas.PruebasIntegracion.Infraestructura.UsuariosPrueba;

namespace PlataformaReservas.PruebasIntegracion.Identidad;

[Collection("BaseDatos")]
public sealed class PruebasFabricaClaimsUsuario(BaseDatosFixture baseDatos)
{
    [Fact]
    public async Task El_principal_lleva_empresa_id_solo_si_hay_pertenencia()
    {
        ServiceProvider servicios = baseDatos.ServiciosIdentidad;
        await using AsyncServiceScope ambito = servicios.CreateAsyncScope();
        UserManager<Usuario> usuarios = ambito.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
        IUserClaimsPrincipalFactory<Usuario> fabrica = ambito.ServiceProvider.GetRequiredService<IUserClaimsPrincipalFactory<Usuario>>();

        Usuario propietario = Usuario.Crear(CorreoUnico(), new NombrePersona("Propietario"), null, Ahora);
        Usuario cliente = Usuario.Crear(CorreoUnico(), new NombrePersona("Cliente"), null, Ahora);
        (await usuarios.CreateAsync(propietario, "ClaveSegura1!")).Succeeded.Should().BeTrue();
        (await usuarios.CreateAsync(cliente, "ClaveSegura1!")).Succeeded.Should().BeTrue();

        Guid empresaId = await CrearEmpresaConMiembroAsync(ambito.ServiceProvider, propietario.Id);

        ClaimsPrincipal conPertenencia = await fabrica.CreateAsync(propietario);
        ClaimsPrincipal sinPertenencia = await fabrica.CreateAsync(cliente);

        conPertenencia.FindFirstValue(FabricaClaimsUsuario.ClaimEmpresa).Should().Be(empresaId.ToString());
        sinPertenencia.FindFirst(FabricaClaimsUsuario.ClaimEmpresa).Should().BeNull();
    }

    private static async Task<Guid> CrearEmpresaConMiembroAsync(IServiceProvider servicios, Guid usuarioId)
    {
        ContextoDatos db = servicios.GetRequiredService<ContextoDatos>();
        string sufijo = Guid.CreateVersion7().ToString("N");

        Categoria categoria = Categoria.Crear(Guid.CreateVersion7(), new NombreCategoria($"Categoria {sufijo}"), Slug.Desde($"cat-{sufijo}"));
        Empresa empresa = Empresa.Crear(
            Guid.CreateVersion7(), new NombreEmpresa("Empresa Prueba"), Slug.Desde($"empresa-{sufijo}"), categoria.Id,
            null, new Email("contacto@prueba.es"), new Telefono("600000000"), new Direccion("Calle Mayor 1"), new CodigoPostal("28001"), new Ciudad("Madrid"), Ahora);
        MiembroEmpresa miembro = MiembroEmpresa.Crear(
            Guid.CreateVersion7(), empresa.Id, usuarioId, RolMiembro.Propietario, Ahora);

        db.AddRange(categoria, empresa, miembro);
        await db.SaveChangesAsync();
        return empresa.Id;
    }
}
