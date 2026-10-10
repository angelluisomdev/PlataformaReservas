using FluentAssertions;
using PlataformaReservas.Aplicacion.Compartido;

namespace PlataformaReservas.PruebasUnitarias.Compartido;

public sealed class PruebasResultado
{
    [Fact]
    public void Un_exito_no_lleva_codigo_ni_mensaje()
    {
        Resultado resultado = Resultado.Exito();

        resultado.EsExito.Should().BeTrue();
        resultado.Codigo.Should().BeNull();
        resultado.Mensaje.Should().BeNull();
    }

    [Fact]
    public void Un_fallo_lleva_su_codigo_y_su_mensaje()
    {
        Resultado resultado = Resultado.Fallo(CodigoError.NoEncontrado, "No existe.");

        resultado.EsExito.Should().BeFalse();
        resultado.Codigo.Should().Be(CodigoError.NoEncontrado);
        resultado.Mensaje.Should().Be("No existe.");
    }

    [Fact]
    public void Un_exito_con_valor_lo_devuelve()
    {
        Resultado<int> resultado = Resultado<int>.Exito(7);

        resultado.EsExito.Should().BeTrue();
        resultado.Valor.Should().Be(7);
        resultado.Codigo.Should().BeNull();
    }

    [Fact]
    public void Un_fallo_con_valor_no_lleva_valor()
    {
        Resultado<string> resultado = Resultado<string>.Fallo(CodigoError.Conflicto, "Ya existe.");

        resultado.EsExito.Should().BeFalse();
        resultado.Valor.Should().BeNull();
        resultado.Codigo.Should().Be(CodigoError.Conflicto);
        resultado.Mensaje.Should().Be("Ya existe.");
    }
}
