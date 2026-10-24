using FluentAssertions;
using Microsoft.Extensions.Configuration;
using PlataformaReservas.Infraestructura.Correo;

namespace PlataformaReservas.PruebasIntegracion.Correo;

public sealed class PruebasUrlBaseCorreo
{
    [Theory]
    [InlineData("https://reservas.example", "https://reservas.example")]
    [InlineData("https://reservas.example/", "https://reservas.example")]
    [InlineData("http://localhost:5124", "http://localhost:5124")]
    public void Acepta_una_url_http_o_https_absoluta_y_quita_la_barra_final(string configurada, string esperada)
    {
        bool valida = UrlBaseCorreo.IntentarObtener(Configuracion(configurada), out string urlBase);

        valida.Should().BeTrue();
        urlBase.Should().Be(esperada);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("/relativa")]
    [InlineData("reservas.example")]
    [InlineData("file:///etc/passwd")]
    [InlineData("javascript:alert(1)")]
    [InlineData("ftp://reservas.example")]
    public void Rechaza_lo_que_no_es_una_url_http_o_https_absoluta(string? configurada)
    {
        UrlBaseCorreo.IntentarObtener(Configuracion(configurada), out _).Should().BeFalse();
    }

    private static IConfiguration Configuracion(string? urlBase)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [UrlBaseCorreo.Clave] = urlBase })
            .Build();
    }
}
