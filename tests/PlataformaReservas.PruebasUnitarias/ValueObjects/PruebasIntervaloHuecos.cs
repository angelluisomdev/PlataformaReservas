using FluentAssertions;
using PlataformaReservas.Dominio.Compartido;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.PruebasUnitarias.ValueObjects;

public sealed class PruebasIntervaloHuecos
{
    [Theory]
    [InlineData(1)]
    [InlineData(480)]
    public void Admite_los_extremos_del_rango(int valor)
    {
        new IntervaloHuecos(valor).Minutos.Should().Be(valor);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(481)]
    [InlineData(int.MinValue)]
    public void Rechaza_los_valores_fuera_del_rango(int valor)
    {
        Action crear = () => _ = new IntervaloHuecos(valor);

        crear.Should().Throw<DominioException>();
    }

    [Fact]
    public void Desde_sin_valor_devuelve_null()
    {
        IntervaloHuecos.Desde(null).Should().BeNull();
    }

    [Fact]
    public void Desde_con_un_valor_valido_equivale_al_constructor()
    {
        IntervaloHuecos.Desde(1).Should().Be(new IntervaloHuecos(1));
    }

    [Fact]
    public void Desde_con_un_valor_fuera_de_rango_lanza()
    {
        Action crear = () => _ = IntervaloHuecos.Desde(481);

        crear.Should().Throw<DominioException>();
    }
}
