using FluentAssertions;
using PlataformaReservas.Dominio.Compartido;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.PruebasUnitarias.ValueObjects;

public sealed class PruebasTelefono
{
    [Theory]
    [InlineData("600000000")]
    [InlineData("+34 600 000 000")]
    [InlineData("91-123-45-67")]
    public void Admite_digitos_espacios_guiones_y_prefijo(string valor)
    {
        new Telefono(valor).Valor.Should().Be(valor);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("12345")]
    [InlineData("1234567890123456")]
    [InlineData("600 abc 000")]
    [InlineData("600+000000")]
    [InlineData("+34 600 000 000 0000 0")]
    public void Rechaza_vacio_caracteres_no_admitidos_o_numero_de_digitos_fuera_de_rango(string valor)
    {
        Action crear = () => _ = new Telefono(valor);

        crear.Should().Throw<DominioException>();
    }

    [Theory]
    [InlineData("123456")]
    [InlineData("123456789012345")]
    [InlineData("+34 600 000 000 0000")]
    public void Admite_los_limites_de_digitos_y_de_longitud(string valor)
    {
        new Telefono(valor).Valor.Should().Be(valor);
    }

    [Fact]
    public void Recorta_los_espacios_de_alrededor()
    {
        new Telefono("  600000000  ").Valor.Should().Be("600000000");
    }

    [Fact]
    public void Rechaza_un_prefijo_sin_digitos()
    {
        Action crear = () => _ = new Telefono("+");

        crear.Should().Throw<DominioException>();
    }
}
