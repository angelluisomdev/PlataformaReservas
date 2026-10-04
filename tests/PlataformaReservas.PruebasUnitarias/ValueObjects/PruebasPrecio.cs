using System.Globalization;
using FluentAssertions;
using PlataformaReservas.Dominio.Compartido;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.PruebasUnitarias.ValueObjects;

public sealed class PruebasPrecio
{
    [Theory]
    [InlineData("0")]
    [InlineData("18.50")]
    public void Admite_cero_y_dos_decimales(string importe)
    {
        decimal valor = decimal.Parse(importe, CultureInfo.InvariantCulture);

        new Precio(valor).Importe.Should().Be(valor);
    }

    [Theory]
    [InlineData("-0.01")]
    [InlineData("10.555")]
    public void Rechaza_negativos_o_mas_de_dos_decimales(string importe)
    {
        decimal valor = decimal.Parse(importe, CultureInfo.InvariantCulture);

        Action crear = () => _ = new Precio(valor);

        crear.Should().Throw<DominioException>();
    }

    [Fact]
    public void Admite_el_maximo_de_la_columna_y_rechaza_lo_que_lo_supera()
    {
        new Precio(99_999_999.99m).Importe.Should().Be(99_999_999.99m);

        Action crear = () => _ = new Precio(100_000_000m);

        crear.Should().Throw<DominioException>();
    }

    [Fact]
    public void Admite_ceros_decimales_de_sobra()
    {
        new Precio(10.500m).Importe.Should().Be(10.5m);
    }
}
