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
public sealed class PruebasRestablecimientoClave(BaseDatosFixture baseDatos)
{
    private const string ClaveNueva = "ClaveNueva2!";

    [Fact]
    public async Task Con_un_enlace_valido_restablece_la_contrasena()
    {
        string correo = CorreoUnico();
        Usuario usuario = await CrearAsync(baseDatos, correo);

        IdentityResult resultado = await RestablecerAsync(correo, await CodigoValidoAsync(correo), ClaveNueva);

        resultado.Succeeded.Should().BeTrue();
        (await ComprobarClaveAsync(baseDatos, usuario.Id, ClaveNueva)).Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Una_cuenta_inexistente_y_un_enlace_invalido_dan_la_misma_respuesta()
    {
        string existente = CorreoUnico();
        await CrearAsync(baseDatos, existente);
        string codigoAjeno = Codificar("token-inventado");

        IdentityResult sinCuenta = await RestablecerAsync(CorreoUnico(), codigoAjeno, ClaveNueva);
        IdentityResult enlaceInvalido = await RestablecerAsync(existente, codigoAjeno, ClaveNueva);

        sinCuenta.Succeeded.Should().BeFalse();
        enlaceInvalido.Succeeded.Should().BeFalse();
        sinCuenta.Errors.Should().BeEquivalentTo(enlaceInvalido.Errors);
        sinCuenta.Errors.Should().ContainSingle().Which.Code.Should().Be(RestablecimientoClave.CodigoEnlaceNoValido);
    }

    [Fact]
    public async Task Un_enlace_invalido_no_revela_los_errores_de_la_politica_de_contrasenas()
    {
        string correo = CorreoUnico();
        await CrearAsync(baseDatos, correo);

        IdentityResult resultado = await RestablecerAsync(correo, Codificar("token-inventado"), "corta");

        resultado.Errors.Should().ContainSingle().Which.Code.Should().Be(RestablecimientoClave.CodigoEnlaceNoValido);
    }

    [Fact]
    public async Task Con_un_enlace_valido_y_una_contrasena_debil_devuelve_los_errores_de_la_politica()
    {
        string correo = CorreoUnico();
        Usuario usuario = await CrearAsync(baseDatos, correo);

        IdentityResult resultado = await RestablecerAsync(correo, await CodigoValidoAsync(correo), "corta");

        resultado.Succeeded.Should().BeFalse();
        resultado.Errors.Should().NotContain(e => e.Code == RestablecimientoClave.CodigoEnlaceNoValido);
        (await ComprobarClaveAsync(baseDatos, usuario.Id, Clave)).Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Un_codigo_malformado_da_enlace_no_valido_sin_lanzar()
    {
        string correo = CorreoUnico();
        await CrearAsync(baseDatos, correo);

        Func<Task<IdentityResult>> restablecer = () => RestablecerAsync(correo, "%%%", ClaveNueva);

        (await restablecer.Should().NotThrowAsync()).Subject.Errors
            .Should().ContainSingle().Which.Code.Should().Be(RestablecimientoClave.CodigoEnlaceNoValido);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("%%%")]
    [InlineData("a")]
    public void Un_codigo_vacio_o_que_no_es_base64url_no_se_decodifica(string? codigo)
    {
        RestablecimientoClave.IntentarDecodificar(codigo, out string token).Should().BeFalse();
        token.Should().BeEmpty();
    }

    [Fact]
    public void Un_codigo_base64url_se_decodifica()
    {
        RestablecimientoClave.IntentarDecodificar(Codificar("abc+/="), out string token).Should().BeTrue();
        token.Should().Be("abc+/=");
    }

    private static string Codificar(string token) => WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

    private async Task<string> CodigoValidoAsync(string correo)
    {
        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        UserManager<Usuario> usuarios = ambito.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
        Usuario usuario = (await usuarios.FindByEmailAsync(correo))!;
        return Codificar(await usuarios.GeneratePasswordResetTokenAsync(usuario));
    }

    private async Task<IdentityResult> RestablecerAsync(string correo, string codigo, string clave)
    {
        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        return await ambito.ServiceProvider.GetRequiredService<RestablecimientoClave>().RestablecerAsync(correo, codigo, clave);
    }
}
