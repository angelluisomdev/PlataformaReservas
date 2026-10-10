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
}
