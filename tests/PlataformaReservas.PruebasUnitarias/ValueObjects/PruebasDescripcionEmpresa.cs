using FluentAssertions;
using PlataformaReservas.Dominio.Compartido;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.PruebasUnitarias.ValueObjects;

public sealed class PruebasDescripcionEmpresa
{
    [Fact]
    public void Recorta_los_espacios_de_alrededor()
    {
        new DescripcionEmpresa("  Cortes y peinados  ").Valor.Should().Be("Cortes y peinados");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rechaza_un_texto_vacio(string valor)
    {
        Action crear = () => _ = new DescripcionEmpresa(valor);

        crear.Should().Throw<DominioException>();
    }

    [Fact]
    public void Admite_la_longitud_maxima_aunque_lleve_espacios_alrededor()
    {
        string maximo = new('x', DescripcionEmpresa.LongitudMaxima);

        new DescripcionEmpresa($"  {maximo}  ").Valor.Should().Be(maximo);
    }

    [Fact]
    public void Rechaza_un_caracter_mas_de_la_longitud_maxima()
    {
        Action crear = () => _ = new DescripcionEmpresa(new string('x', DescripcionEmpresa.LongitudMaxima + 1));

        crear.Should().Throw<DominioException>();
    }

    [Fact]
    public void Dos_textos_iguales_tras_recortar_son_iguales()
    {
        new DescripcionEmpresa("Cortes y peinados").Should().Be(new DescripcionEmpresa(" Cortes y peinados "));
    }
}
