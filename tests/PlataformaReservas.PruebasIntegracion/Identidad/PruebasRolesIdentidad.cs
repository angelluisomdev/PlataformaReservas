using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using PlataformaReservas.Infraestructura.Identidad;
using PlataformaReservas.PruebasIntegracion.Infraestructura;

namespace PlataformaReservas.PruebasIntegracion.Identidad;

[Collection("BaseDatos")]
public sealed class PruebasRolesIdentidad(BaseDatosFixture baseDatos)
{
    [Fact]
    public async Task Los_roles_cliente_y_propietario_existen_por_migracion()
    {
        ServiceProvider servicios = baseDatos.ServiciosIdentidad;
        await using AsyncServiceScope ambito = servicios.CreateAsyncScope();
        RoleManager<IdentityRole<Guid>> roles = ambito.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        (await roles.RoleExistsAsync(RolesIdentidad.Cliente)).Should().BeTrue();
        (await roles.RoleExistsAsync(RolesIdentidad.Propietario)).Should().BeTrue();
    }
}
