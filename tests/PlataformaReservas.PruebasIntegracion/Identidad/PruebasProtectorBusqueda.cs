using FluentAssertions;
using PlataformaReservas.Infraestructura.Identidad;
using PlataformaReservas.PruebasIntegracion.Infraestructura;

namespace PlataformaReservas.PruebasIntegracion.Identidad;

public sealed class PruebasProtectorBusqueda
{
    [Fact]
    public void El_valor_de_busqueda_es_determinista_y_distingue_entradas()
    {
        AnilloClaves anillo = ClavesPrueba.Anillo();
        ProtectorBusqueda protector = new(anillo);

        string? primero = protector.Protect(anillo.CurrentKeyId, "ANA@EJEMPLO.COM");
        string? segundo = protector.Protect(anillo.CurrentKeyId, "ANA@EJEMPLO.COM");
        string? otro = protector.Protect(anillo.CurrentKeyId, "LUIS@EJEMPLO.COM");

        primero.Should().Be(segundo);
        primero.Should().NotBe(otro);
        primero.Should().NotContain("EJEMPLO");
    }

    [Fact]
    public void No_se_puede_deshacer_el_valor_de_busqueda()
    {
        AnilloClaves anillo = ClavesPrueba.Anillo();
        ProtectorBusqueda protector = new(anillo);

        Action deshacer = () => protector.Unprotect(anillo.CurrentKeyId, "cualquiera");

        deshacer.Should().Throw<NotSupportedException>();
    }
}
