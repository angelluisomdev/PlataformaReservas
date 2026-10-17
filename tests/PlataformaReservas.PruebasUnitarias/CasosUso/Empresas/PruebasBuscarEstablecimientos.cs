using FluentAssertions;
using PlataformaReservas.Aplicacion.CasosUso.Empresas;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Aplicacion.Dtos.Empresas;

namespace PlataformaReservas.PruebasUnitarias.CasosUso.Empresas;

public sealed class PruebasBuscarEstablecimientos
{
    private readonly CatalogoPublicoFalso catalogo = new();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("E")]
    [InlineData(" E ")]
    [InlineData("Ciudad de mas de ochenta caracteres que nunca puede coincidir con ninguna guardada xx")]
    public async Task Sin_ciudad_o_con_menos_de_dos_caracteres_devuelve_vacio_sin_consultar(string? ciudad)
    {
        Resultado<PaginaResultados<ResumenEmpresaDto>> resultado = await BuscarAsync(ciudad);

        resultado.EsExito.Should().BeTrue();
        resultado.Valor!.Elementos.Should().BeEmpty();
        resultado.Valor.Total.Should().Be(0);
        catalogo.Busquedas.Should().BeEmpty();
    }

    [Fact]
    public async Task Una_ciudad_de_dos_caracteres_si_consulta_y_llega_recortada()
    {
        await BuscarAsync("  Ea  ");

        catalogo.Busquedas.Should().ContainSingle().Which.Ciudad.Should().Be("Ea");
    }

    [Theory]
    [InlineData(100, 20)]
    [InlineData(21, 20)]
    [InlineData(20, 20)]
    [InlineData(5, 5)]
    [InlineData(0, 20)]
    [InlineData(-3, 20)]
    [InlineData(null, 20)]
    public async Task El_tamano_de_pagina_nunca_supera_el_maximo_del_servidor(int? pedido, int aplicado)
    {
        await BuscarAsync("Madrid", tamano: pedido);

        catalogo.Busquedas.Should().ContainSingle().Which.TamanoPagina.Should().Be(aplicado);
    }

    [Theory]
    [InlineData(null, 1)]
    [InlineData(0, 1)]
    [InlineData(-2, 1)]
    [InlineData(3, 3)]
    public async Task La_pagina_empieza_en_uno(int? pedida, int aplicada)
    {
        await BuscarAsync("Madrid", pagina: pedida);

        catalogo.Busquedas.Should().ContainSingle().Which.Pagina.Should().Be(aplicada);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("  ", null)]
    [InlineData(" barberia ", "barberia")]
    public async Task Una_categoria_en_blanco_no_filtra(string? categoria, string? esperada)
    {
        await BuscarAsync("Madrid", categoria);

        catalogo.Busquedas.Should().ContainSingle().Which.Categoria.Should().Be(esperada);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData(" E ", false)]
    [InlineData("Ea", true)]
    [InlineData(" Málaga ", true)]
    [InlineData("Ciudad de mas de ochenta caracteres que nunca puede coincidir con ninguna guardada xx", false)]
    public void EsCiudadValida_exige_dos_caracteres_tras_recortar(string? ciudad, bool valida)
    {
        BuscarEstablecimientos.EsCiudadValida(ciudad).Should().Be(valida);
    }

    private Task<Resultado<PaginaResultados<ResumenEmpresaDto>>> BuscarAsync(
        string? ciudad, string? categoria = null, int? pagina = null, int? tamano = null)
    {
        return new BuscarEstablecimientos(catalogo).EjecutarAsync(
            new BuscarEstablecimientosComando(ciudad, categoria, pagina, tamano), CancellationToken.None);
    }
}
