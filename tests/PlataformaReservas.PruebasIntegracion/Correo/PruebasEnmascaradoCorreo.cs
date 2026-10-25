using FluentAssertions;
using PlataformaReservas.Infraestructura.Correo;

namespace PlataformaReservas.PruebasIntegracion.Correo;

public sealed class PruebasEnmascaradoCorreo
{
    [Theory]
    [InlineData("ana@ejemplo.es", "a***@ejemplo.es")]
    [InlineData("a@b.es", "a***@b.es")]
    [InlineData("sin-arroba", "***")]
    [InlineData("@ejemplo.es", "***")]
    [InlineData("", "***")]
    public void Enmascara_todo_salvo_la_primera_letra_y_el_dominio(string correo, string esperado)
    {
        EnmascaradoCorreo.Enmascarar(correo).Should().Be(esperado);
    }
}
