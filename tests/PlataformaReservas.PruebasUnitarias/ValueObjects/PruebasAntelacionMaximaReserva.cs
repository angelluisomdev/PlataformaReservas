using FluentAssertions;
using PlataformaReservas.Dominio.Compartido;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.PruebasUnitarias.ValueObjects;

public sealed class PruebasAntelacionMaximaReserva
{
    [Theory]
    [InlineData(1)]
    [InlineData(365)]
    public void Admite_los_extremos_del_rango(int valor)
    {
        new AntelacionMaximaReserva(valor).Dias.Should().Be(valor);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(366)]
    [InlineData(int.MinValue)]
    public void Rechaza_los_valores_fuera_del_rango(int valor)
    {
        Action crear = () => _ = new AntelacionMaximaReserva(valor);

        crear.Should().Throw<DominioException>();
    }

    [Fact]
    public void Desde_sin_valor_devuelve_null()
    {
        AntelacionMaximaReserva.Desde(null).Should().BeNull();
    }

    [Fact]
    public void Desde_con_un_valor_valido_equivale_al_constructor()
    {
        AntelacionMaximaReserva.Desde(1).Should().Be(new AntelacionMaximaReserva(1));
    }

    [Fact]
    public void Desde_con_un_valor_fuera_de_rango_lanza()
    {
        Action crear = () => _ = AntelacionMaximaReserva.Desde(366);

        crear.Should().Throw<DominioException>();
    }
}
