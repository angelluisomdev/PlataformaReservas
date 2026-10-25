using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.HtmlRendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using PlataformaReservas.Web.Components.Compartido;

namespace PlataformaReservas.PruebasIntegracion.Components.Compartido;

public sealed class PruebasDialogo
{
    [Fact]
    public async Task Cerrado_no_pinta_nada()
    {
        string html = await RenderizarAsync("Editar servicio", abierto: false);

        html.Should().BeEmpty();
    }

    [Fact]
    public async Task Abierto_pinta_una_ventana_modal_con_su_titulo()
    {
        string html = await RenderizarAsync("Editar servicio", abierto: true);

        html.Should().Contain("role=\"dialog\"").And.Contain("aria-modal=\"true\"").And.Contain(">Editar servicio</h2>");
    }

    [Fact]
    public async Task El_titulo_se_muestra_escapado()
    {
        string html = await RenderizarAsync("Desactivar <script>alert(1)</script>", abierto: true);

        html.Should().Contain("Desactivar &lt;script&gt;").And.NotContain("<script");
    }

    private static async Task<string> RenderizarAsync(string titulo, bool abierto)
    {
        await using ServiceProvider proveedor = new ServiceCollection().BuildServiceProvider();
        await using HtmlRenderer renderizador = new(proveedor, NullLoggerFactory.Instance);

        return await renderizador.Dispatcher.InvokeAsync(async () =>
        {
            HtmlRootComponent raiz = await renderizador.RenderComponentAsync<Dialogo>(
                ParameterView.FromDictionary(new Dictionary<string, object?>
                {
                    [nameof(Dialogo.Titulo)] = titulo,
                    [nameof(Dialogo.Abierto)] = abierto,
                }));
            return raiz.ToHtmlString().Trim();
        });
    }
}
