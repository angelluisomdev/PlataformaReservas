using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using PlataformaReservas.Infraestructura.Identidad;
using PlataformaReservas.PruebasIntegracion.Infraestructura;
using static PlataformaReservas.PruebasIntegracion.Infraestructura.UsuariosPrueba;

namespace PlataformaReservas.PruebasIntegracion.Identidad;

[Collection("BaseDatos")]
public sealed class PruebasConfirmacionUsuarioActivo(BaseDatosFixture baseDatos)
{
    [Fact]
    public async Task Un_usuario_inactivo_no_puede_iniciar_sesion()
    {
        string correo = CorreoUnico();
        Usuario usuario = await CrearAsync(baseDatos, correo);

        SignInResult activo = await ComprobarClaveAsync(baseDatos, usuario.Id, Clave);
        await DesactivarAsync(baseDatos, usuario.Id);
        SignInResult inactivo = await ComprobarClaveAsync(baseDatos, usuario.Id, Clave);

        activo.Succeeded.Should().BeTrue();
        inactivo.Succeeded.Should().BeFalse();
        inactivo.IsNotAllowed.Should().BeTrue();
    }
}
