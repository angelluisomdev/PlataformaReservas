using FluentAssertions;
using PlataformaReservas.Aplicacion.Compartido;

namespace PlataformaReservas.PruebasUnitarias.Compartido;

public sealed class PruebasResultadoT
{
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
