using FluentAssertions;
using PlataformaReservas.Dominio.Compartido;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.PruebasUnitarias.ValueObjects;

public sealed class PruebasMotivoExcepcion
{
    [Fact]
    public void Recorta_los_espacios_de_alrededor()
    {
        new MotivoExcepcion("  Vacaciones  ").Valor.Should().Be("Vacaciones");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rechaza_un_texto_vacio(string valor)
    {
        Action crear = () => _ = new MotivoExcepcion(valor);

        crear.Should().Throw<DominioException>();
    }

    [Fact]
    public void Admite_la_longitud_maxima_y_rechaza_un_caracter_mas()
    {
        string maximo = new('x', MotivoExcepcion.LongitudMaxima);

        new MotivoExcepcion(maximo).Valor.Should().Be(maximo);
        Action crear = () => _ = new MotivoExcepcion(maximo + "x");
        crear.Should().Throw<DominioException>();
    }

    [Fact]
    public void La_longitud_maxima_se_mide_tras_recortar()
    {
        string maximo = new('x', MotivoExcepcion.LongitudMaxima);

        new MotivoExcepcion($"   {maximo}   ").Valor.Should().Be(maximo);
    }
}
