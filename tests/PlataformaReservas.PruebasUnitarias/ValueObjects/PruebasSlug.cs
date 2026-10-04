using FluentAssertions;
using PlataformaReservas.Dominio.Compartido;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.PruebasUnitarias.ValueObjects;

public sealed class PruebasSlug
{
    [Theory]
    [InlineData("Peluquería Ana", "peluqueria-ana")]
    [InlineData("  Barbería   El  Niño!! ", "barberia-el-nino")]
    [InlineData("Taller 2000", "taller-2000")]
    public void Normaliza_el_texto(string texto, string esperado)
    {
        Slug.Desde(texto).Valor.Should().Be(esperado);
    }

    [Theory]
    [InlineData("")]
    [InlineData("¡!")]
    [InlineData("ab")]
    public void Rechaza_textos_sin_caracteres_suficientes(string texto)
    {
        Action crear = () => Slug.Desde(texto);

        crear.Should().Throw<DominioException>();
    }

    [Fact]
    public void Con_sufijo_anade_el_numero()
    {
        Slug.Desde("peluqueria-ana").ConSufijo(2).Valor.Should().Be("peluqueria-ana-2");
    }

    [Fact]
    public void Con_sufijo_no_supera_la_longitud_maxima()
    {
        Slug largo = Slug.Desde(new string('a', Slug.LongitudMaxima));

        largo.ConSufijo(12).Valor.Length.Should().BeLessThanOrEqualTo(Slug.LongitudMaxima);
    }

    [Fact]
    public void Admite_tres_caracteres()
    {
        Slug.Desde("abc").Valor.Should().Be("abc");
    }

    [Fact]
    public void Recorta_un_texto_largo_sin_dejar_un_guion_al_final()
    {
        Slug slug = Slug.Desde(new string('a', Slug.LongitudMaxima - 1) + " b");

        slug.Valor.Length.Should().BeLessThanOrEqualTo(Slug.LongitudMaxima);
        slug.Valor.Should().NotEndWith("-");
    }

    [Fact]
    public void Con_sufijo_negativo_rechaza_el_slug_resultante()
    {
        Action crear = () => Slug.Desde("peluqueria-ana").ConSufijo(-1);

        crear.Should().Throw<DominioException>();
    }

    [Fact]
    public void Dos_slugs_del_mismo_texto_son_iguales()
    {
        Slug.Desde("Peluquería Ana").Should().Be(Slug.Desde("peluqueria-ana"));
    }
}
