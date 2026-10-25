using FluentAssertions;
using PlataformaReservas.PruebasIntegracion.Infraestructura;
using PlataformaReservas.Web.Cuenta;

namespace PlataformaReservas.PruebasIntegracion.Cuenta;

public sealed class PruebasIdentityRedirectManager
{
    [Theory]
    [InlineData("//evil.com")]
    [InlineData("//evil.com/app")]
    [InlineData("/\\evil.com")]
    [InlineData("/\\/evil.com")]
    [InlineData("\\evil.com")]
    [InlineData("/\t/evil.com")]
    [InlineData(" //evil.com")]
    [InlineData("\t\\\\evil.com")]
    [InlineData("https://evil.com")]
    [InlineData("http://reservas.example.evil.com/app")]
    [InlineData("javascript:alert(1)")]
    [InlineData(NavegacionPrueba.Base + "/evil.com")]
    [InlineData(NavegacionPrueba.Base + "//evil.com")]
    [InlineData(NavegacionPrueba.Base + "\\evil.com")]
    [InlineData(NavegacionPrueba.Base + "\\\\evil.com")]
    [InlineData("HTTPS://RESERVAS.EXAMPLE//evil.com")]
    public void Un_destino_fuera_de_la_aplicacion_redirige_a_la_raiz(string destino)
    {
        Redirigir(destino).Should().Be("/");
    }

    [Theory]
    [InlineData("/app", "/app")]
    [InlineData("/my-account/bookings?x=1", "/my-account/bookings?x=1")]
    [InlineData("Account/ResetPasswordConfirmation", "Account/ResetPasswordConfirmation")]
    [InlineData("app", "app")]
    [InlineData("", "")]
    [InlineData(null, "")]
    [InlineData(NavegacionPrueba.Base + "my-account/bookings?x=1", "/my-account/bookings?x=1")]
    [InlineData(NavegacionPrueba.Base + "app#seccion", "/app#seccion")]
    [InlineData("HTTPS://RESERVAS.EXAMPLE/app", "/app")]
    public void Un_destino_de_la_aplicacion_se_conserva(string? destino, string esperado)
    {
        Redirigir(destino).Should().Be(esperado);
    }

    [Fact]
    public void Con_parametros_de_consulta_redirige_dentro_de_la_aplicacion()
    {
        NavegacionPrueba navegacion = new();

        new IdentityRedirectManager(navegacion).RedirectTo("Account/Login", new Dictionary<string, object?> { ["ReturnUrl"] = "/app" });

        navegacion.Destino.Should().Be("/Account/Login?ReturnUrl=%2Fapp");
    }

    private static string? Redirigir(string? destino)
    {
        NavegacionPrueba navegacion = new();
        new IdentityRedirectManager(navegacion).RedirectTo(destino);
        return navegacion.Destino;
    }
}
