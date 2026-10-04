using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using PlataformaReservas.Infraestructura.Identidad;
using PlataformaReservas.PruebasIntegracion.Infraestructura;
using static PlataformaReservas.PruebasIntegracion.Infraestructura.UsuariosPrueba;

namespace PlataformaReservas.PruebasIntegracion.Identidad;

[Collection("BaseDatos")]
public sealed class PruebasSolicitudRestablecimientoClave(BaseDatosFixture baseDatos)
{
    [Fact]
    public async Task La_solicitud_de_restablecimiento_responde_igual_exista_o_no_la_cuenta()
    {
        string existente = CorreoUnico();
        string inexistente = CorreoUnico();
        await CrearAsync(baseDatos, existente);

        Func<Task> conCuenta = () => SolicitarRestablecimientoAsync(existente);
        Func<Task> sinCuenta = () => SolicitarRestablecimientoAsync(inexistente);

        await conCuenta.Should().NotThrowAsync();
        await sinCuenta.Should().NotThrowAsync();
        baseDatos.Remitente.EnlaceDe(existente).Should().NotBeNull();
        baseDatos.Remitente.EnlaceDe(inexistente).Should().BeNull();
    }

    [Fact]
    public async Task Una_cuenta_inactiva_no_genera_enlace_de_restablecimiento()
    {
        string correo = CorreoUnico();
        Usuario usuario = await CrearAsync(baseDatos, correo);
        await DesactivarAsync(baseDatos, usuario.Id);

        await SolicitarRestablecimientoAsync(correo);

        baseDatos.Remitente.EnlaceDe(correo).Should().BeNull();
    }

    [Fact]
    public async Task Restablecer_la_contrasena_renueva_el_sello_de_seguridad()
    {
        string correo = CorreoUnico();
        Usuario creado = await CrearAsync(baseDatos, correo);
        await SolicitarRestablecimientoAsync(correo);
        string token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(baseDatos.Remitente.EnlaceDe(correo)!));

        await using (AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope())
        {
            UserManager<Usuario> usuarios = ambito.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
            Usuario usuario = (await usuarios.FindByEmailAsync(correo))!;
            string selloAntes = (await usuarios.GetSecurityStampAsync(usuario))!;

            IdentityResult restablecido = await usuarios.ResetPasswordAsync(usuario, token, "ClaveNueva2!");

            restablecido.Succeeded.Should().BeTrue();
            (await usuarios.GetSecurityStampAsync(usuario)).Should().NotBe(selloAntes);
        }

        (await ComprobarClaveAsync(baseDatos, creado.Id, "ClaveNueva2!")).Succeeded.Should().BeTrue();
        (await ComprobarClaveAsync(baseDatos, creado.Id, Clave)).Succeeded.Should().BeFalse();
    }

    private async Task SolicitarRestablecimientoAsync(string correo)
    {
        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        SolicitudRestablecimientoClave solicitud = ambito.ServiceProvider.GetRequiredService<SolicitudRestablecimientoClave>();

        await solicitud.SolicitarAsync(correo, codigo => codigo);
    }
}
