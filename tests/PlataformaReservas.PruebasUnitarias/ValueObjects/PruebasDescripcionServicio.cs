using FluentAssertions;
using PlataformaReservas.Dominio.Compartido;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.PruebasUnitarias.ValueObjects;

public sealed class PruebasDescripcionServicio
{
    [Fact]
    public void Recorta_los_espacios_de_alrededor()
    {
        new DescripcionServicio("  Corte a tijera  ").Valor.Should().Be("Corte a tijera");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rechaza_un_texto_vacio(string valor)
    {
        Action crear = () => _ = new DescripcionServicio(valor);

        crear.Should().Throw<DominioException>();
    }

    [Fact]
    public void Admite_la_longitud_maxima_y_rechaza_un_caracter_mas()
    {
        string maximo = new('x', DescripcionServicio.LongitudMaxima);

        new DescripcionServicio(maximo).Valor.Should().Be(maximo);
        Action crear = () => _ = new DescripcionServicio(maximo + "x");
        crear.Should().Throw<DominioException>();
    }
}
