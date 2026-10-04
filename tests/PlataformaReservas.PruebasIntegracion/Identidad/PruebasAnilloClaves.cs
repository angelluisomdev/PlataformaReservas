using FluentAssertions;
using PlataformaReservas.Infraestructura.Identidad;
using PlataformaReservas.PruebasIntegracion.Infraestructura;

namespace PlataformaReservas.PruebasIntegracion.Identidad;

public sealed class PruebasAnilloClaves
{
    [Fact]
    public void El_anillo_rechaza_claves_iguales()
    {
        string clave = BaseDatosFixture.ClaveAleatoria();

        Action crear = () => ClavesPrueba.Anillo(clave, clave);

        crear.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("no-es-base64")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAA==")]
    public void El_anillo_rechaza_claves_que_no_son_de_256_bits(string clave)
    {
        Action crear = () => ClavesPrueba.Anillo(claveBusqueda: clave);

        crear.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Solo_expone_la_clave_actual()
    {
        AnilloClaves anillo = ClavesPrueba.Anillo();

        anillo.GetAllKeyIds().Should().ContainSingle().Which.Should().Be(anillo.CurrentKeyId);
        anillo[anillo.CurrentKeyId].Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Una_clave_desconocida_lanza()
    {
        AnilloClaves anillo = ClavesPrueba.Anillo();

        Action leer = () => _ = anillo["otra"];

        leer.Should().Throw<KeyNotFoundException>();
    }
}
