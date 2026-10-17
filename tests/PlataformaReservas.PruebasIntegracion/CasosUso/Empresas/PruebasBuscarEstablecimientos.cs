using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using PlataformaReservas.Aplicacion.CasosUso.Empresas;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Aplicacion.Dtos.Empresas;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.ValueObjects;
using PlataformaReservas.PruebasIntegracion.Infraestructura;

namespace PlataformaReservas.PruebasIntegracion.CasosUso.Empresas;

[Collection("BaseDatos")]
public sealed class PruebasBuscarEstablecimientos(BaseDatosFixture baseDatos)
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("E")]
    public async Task Sin_ciudad_o_con_un_solo_caracter_no_devuelve_nada_aunque_haya_empresas(string? ciudad)
    {
        Categoria categoria = await EscenarioEmpresa.CrearCategoriaAsync(baseDatos);
        await CrearPublicableAsync(categoria.Id, "E");

        Resultado<PaginaResultados<ResumenEmpresaDto>> resultado = await BuscarAsync(ciudad, categoria.Slug.Valor);

        resultado.EsExito.Should().BeTrue();
        resultado.Valor!.Elementos.Should().BeEmpty();
        resultado.Valor.Total.Should().Be(0);
    }

    [Fact]
    public async Task Busca_sin_contexto_de_empresa_y_limita_el_tamano_pedido_por_el_cliente()
    {
        Categoria categoria = await EscenarioEmpresa.CrearCategoriaAsync(baseDatos);
        string ciudad = $"Ciudad {DominioPrueba.Sufijo()}";
        await CrearPublicableAsync(categoria.Id, ciudad);

        Resultado<PaginaResultados<ResumenEmpresaDto>> resultado = await BuscarAsync($"  {ciudad}  ", null, tamano: 100);

        resultado.Valor!.Elementos.Should().ContainSingle();
        resultado.Valor.TamanoPagina.Should().Be(BuscarEstablecimientos.TamanoPaginaMaximo);
    }

    private async Task CrearPublicableAsync(Guid categoriaId, string ciudad)
    {
        Empresa empresa = DominioPrueba.Empresa(DominioPrueba.Sufijo(), categoriaId, ciudad: new Ciudad(ciudad));
        Servicio servicio = DominioPrueba.Servicio(empresa.Id);
        Profesional profesional = EscenarioReserva.ProfesionalQuePresta(empresa.Id, servicio.Id);
        await EscenarioEmpresa.GuardarAsync(baseDatos, empresa, servicio, profesional);
    }

    private async Task<Resultado<PaginaResultados<ResumenEmpresaDto>>> BuscarAsync(
        string? ciudad, string? categoria, int? tamano = null)
    {
        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        return await ambito.ServiceProvider.GetRequiredService<BuscarEstablecimientos>().EjecutarAsync(
            new BuscarEstablecimientosComando(ciudad, categoria, null, tamano), CancellationToken.None);
    }
}
