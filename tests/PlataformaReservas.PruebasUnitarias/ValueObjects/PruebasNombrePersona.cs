using FluentAssertions;
using PlataformaReservas.Dominio.Compartido;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.PruebasUnitarias.ValueObjects;

public sealed class PruebasNombrePersona
{
    [Fact]
    public void Recorta_los_espacios_de_alrededor()
    {
        new NombrePersona("  Lucia Perez  ").Valor.Should().Be("Lucia Perez");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rechaza_un_texto_vacio(string valor)
    {
        Action crear = () => _ = new NombrePersona(valor);

        crear.Should().Throw<DominioException>();
    }

    [Fact]
    public void Admite_la_longitud_maxima_aunque_lleve_espacios_alrededor()
    {
        string maximo = new('x', NombrePersona.LongitudMaxima);

        new NombrePersona($"  {maximo}  ").Valor.Should().Be(maximo);
    }

    [Fact]
    public void Rechaza_un_caracter_mas_de_la_longitud_maxima()
    {
        Action crear = () => _ = new NombrePersona(new string('x', NombrePersona.LongitudMaxima + 1));

        crear.Should().Throw<DominioException>();
    }

    [Fact]
    public void Dos_textos_iguales_tras_recortar_son_iguales()
    {
        new NombrePersona("Lucia Perez").Should().Be(new NombrePersona(" Lucia Perez "));
    }
}
