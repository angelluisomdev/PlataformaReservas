using FluentAssertions;
using PlataformaReservas.Dominio.Compartido;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.PruebasUnitarias.ValueObjects;

public sealed class PruebasDuracion
{
    [Theory]
    [InlineData(0)]
    [InlineData(-15)]
    public void Rechaza_cero_o_negativa(int minutos)
    {
        Action crear = () => _ = new Duracion(minutos);

        crear.Should().Throw<DominioException>();
    }

    [Fact]
    public void Admite_minutos_positivos()
    {
        new Duracion(45).Minutos.Should().Be(45);
    }
}
