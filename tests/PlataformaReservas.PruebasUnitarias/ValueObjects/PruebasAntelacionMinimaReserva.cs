using FluentAssertions;
using PlataformaReservas.Dominio.Compartido;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.PruebasUnitarias.ValueObjects;

public sealed class PruebasAntelacionMinimaReserva
{
    [Theory]
    [InlineData(0)]
    [InlineData(43200)]
    public void Admite_los_extremos_del_rango(int valor)
    {
        new AntelacionMinimaReserva(valor).Minutos.Should().Be(valor);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(43201)]
    [InlineData(int.MinValue)]
    public void Rechaza_los_valores_fuera_del_rango(int valor)
    {
        Action crear = () => _ = new AntelacionMinimaReserva(valor);

        crear.Should().Throw<DominioException>();
    }

    [Fact]
    public void Desde_sin_valor_devuelve_null()
    {
        AntelacionMinimaReserva.Desde(null).Should().BeNull();
    }

    [Fact]
    public void Desde_con_un_valor_valido_equivale_al_constructor()
    {
        AntelacionMinimaReserva.Desde(0).Should().Be(new AntelacionMinimaReserva(0));
    }

    [Fact]
    public void Desde_con_un_valor_fuera_de_rango_lanza()
    {
        Action crear = () => _ = AntelacionMinimaReserva.Desde(-1);

        crear.Should().Throw<DominioException>();
    }
}
