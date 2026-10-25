using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.HtmlRendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.ValueObjects;
using PlataformaReservas.PruebasIntegracion.Infraestructura;
using PlataformaReservas.Web.Components.Publico;

namespace PlataformaReservas.PruebasIntegracion.Components.Publico;

[Collection("BaseDatos")]
public sealed class PruebasFichaEstablecimiento(BaseDatosFixture baseDatos)
{
    private const string Inyeccion = "<script>alert(1)</script>";

    [Fact]
    public async Task El_texto_del_propietario_con_script_se_muestra_escapado()
    {
        Empresa empresa = await CrearPublicableAsync(new NombreEmpresa($"Barberia {Inyeccion}"), new DescripcionEmpresa($"Somos {Inyeccion}"));

        string html = await RenderizarAsync(empresa.Slug.Valor);

        html.Should().Contain("Somos &lt;script&gt;alert(1)&lt;/script&gt;");
        html.Should().Contain("Barberia &lt;script&gt;");
        html.Should().NotContain("<script");
    }

    [Fact]
    public async Task Una_empresa_publicable_muestra_su_ficha()
    {
        Empresa empresa = await CrearPublicableAsync(new NombreEmpresa("Barberia Centro"), new DescripcionEmpresa("Cortes clasicos"));

        string html = await RenderizarAsync(empresa.Slug.Valor);

        html.Should().Contain(">Barberia Centro</h1>").And.Contain("Cortes clasicos").And.Contain($"book/{empresa.Slug.Valor}");
    }

    private async Task<Empresa> CrearPublicableAsync(NombreEmpresa nombre, DescripcionEmpresa descripcion)
    {
        Categoria categoria = await EscenarioEmpresa.CrearCategoriaAsync(baseDatos);
        Empresa empresa = DominioPrueba.Empresa(DominioPrueba.Sufijo(), categoria.Id, descripcion, nombre);
        Servicio servicio = DominioPrueba.Servicio(empresa.Id);
        Profesional profesional = EscenarioReserva.ProfesionalQuePresta(empresa.Id, servicio.Id);
        await EscenarioEmpresa.GuardarAsync(baseDatos, empresa, servicio, profesional);
        return empresa;
    }

    private async Task<string> RenderizarAsync(string slug)
    {
        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        ServiceCollection servicios = new();
        servicios.AddSingleton(ambito.ServiceProvider.GetRequiredService<IConsultasCatalogoPublico>());
        servicios.AddSingleton<NavigationManager>(new NavegacionPrueba($"businesses/{slug}"));
        await using ServiceProvider proveedor = servicios.BuildServiceProvider();
        await using HtmlRenderer renderizador = new(proveedor, NullLoggerFactory.Instance);

        return await renderizador.Dispatcher.InvokeAsync(async () =>
        {
            HtmlRootComponent raiz = await renderizador.RenderComponentAsync<FichaEstablecimiento>(
                ParameterView.FromDictionary(new Dictionary<string, object?> { [nameof(FichaEstablecimiento.Slug)] = slug }));
            return raiz.ToHtmlString();
        });
    }
}
