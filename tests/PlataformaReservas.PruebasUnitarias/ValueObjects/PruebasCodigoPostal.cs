using FluentAssertions;
using PlataformaReservas.Dominio.Compartido;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.PruebasUnitarias.ValueObjects;

public sealed class PruebasCodigoPostal
{
    [Fact]
    public void Admite_cinco_digitos()
    {
        new CodigoPostal(" 28001 ").Valor.Should().Be("28001");
    }

    [Theory]
    [InlineData("")]
    [InlineData("2800")]
    [InlineData("280012")]
    [InlineData("28A01")]
    public void Rechaza_lo_que_no_son_cinco_digitos(string valor)
    {
        Action crear = () => _ = new CodigoPostal(valor);

        crear.Should().Throw<DominioException>();
    }
}
