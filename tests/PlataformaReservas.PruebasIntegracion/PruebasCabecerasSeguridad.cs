using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using PlataformaReservas.Web;

namespace PlataformaReservas.PruebasIntegracion;

public sealed class PruebasCabecerasSeguridad
{
    [Fact]
    public async Task Toda_respuesta_lleva_las_cabeceras_de_seguridad()
    {
        HttpContext contexto = await ResponderAsync(_ => Task.CompletedTask);

        IHeaderDictionary cabeceras = contexto.Response.Headers;
        cabeceras.XContentTypeOptions.ToString().Should().Be("nosniff");
        cabeceras.XFrameOptions.ToString().Should().Be("DENY");
        cabeceras["Referrer-Policy"].ToString().Should().Be("strict-origin-when-cross-origin");
        cabeceras.ContentSecurityPolicy.ToString().Should().Be(CabecerasSeguridad.PoliticaContenido);
    }

    [Fact]
    public async Task Una_respuesta_de_error_o_de_rechazo_tambien_las_lleva()
    {
        HttpContext contexto = await ResponderAsync(c =>
        {
            c.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            return Task.CompletedTask;
        });

        contexto.Response.Headers.ContentSecurityPolicy.ToString().Should().Be(CabecerasSeguridad.PoliticaContenido);
        contexto.Response.Headers.XFrameOptions.ToString().Should().Be("DENY");
    }

    [Fact]
    public void La_politica_de_contenido_es_la_de_arquitectura_y_no_admite_scripts_en_linea_ni_marcos()
    {
        string politica = CabecerasSeguridad.PoliticaContenido;

        politica.Should().Contain("default-src 'self'")
            .And.Contain("script-src 'self' 'wasm-unsafe-eval'")
            .And.Contain("style-src 'self'")
            .And.Contain("img-src 'self' data:")
            .And.Contain("connect-src 'self'")
            .And.Contain("frame-ancestors 'none'")
            .And.Contain("base-uri 'self'");
        politica.Should().NotContain("unsafe-inline").And.NotContain("'unsafe-eval'");
    }

    private static async Task<HttpContext> ResponderAsync(RequestDelegate final)
    {
        await using ServiceProvider servicios = new ServiceCollection().BuildServiceProvider();
        ApplicationBuilder aplicacion = new(servicios);
        aplicacion.UsarCabecerasSeguridad();
        aplicacion.Run(final);

        DefaultHttpContext contexto = new();
        await aplicacion.Build()(contexto);
        return contexto;
    }
}
