using FluentAssertions;
using PlataformaReservas.Dominio.Compartido;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.PruebasUnitarias.ValueObjects;

public sealed class PruebasEmail
{
    [Theory]
    [InlineData("contacto@prueba.es")]
    [InlineData("  ana.garcia@empresa.com  ")]
    public void Admite_un_correo_valido_y_lo_recorta(string valor)
    {
        new Email(valor).Valor.Should().Be(valor.Trim());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("sin-arroba")]
    [InlineData("Ana <ana@prueba.es>")]
    [InlineData("ana@")]
    public void Rechaza_un_correo_vacio_o_mal_formado(string valor)
    {
        Action crear = () => _ = new Email(valor);

        crear.Should().Throw<DominioException>();
    }

    [Fact]
    public void Rechaza_mas_de_160_caracteres()
    {
        string valor = new string('a', 150) + "@prueba.es1";

        Action crear = () => _ = new Email(valor);

        crear.Should().Throw<DominioException>();
    }

    [Fact]
    public void Dos_correos_con_el_mismo_valor_son_iguales()
    {
        new Email("ana@prueba.es").Should().Be(new Email("  ana@prueba.es "));
    }

    [Fact]
    public void Admite_160_caracteres()
    {
        string valor = new string('a', 150) + "@prueba.es";

        new Email(valor).Valor.Should().HaveLength(160);
    }
}
