using System.Security.Cryptography;
using FluentAssertions;
using PlataformaReservas.Infraestructura.Identidad;
using PlataformaReservas.PruebasIntegracion.Infraestructura;

namespace PlataformaReservas.PruebasIntegracion.Identidad;

public sealed class PruebasProtectorDatosPersonales
{
    [Fact]
    public void El_cifrado_va_y_vuelve_con_un_nonce_distinto_cada_vez()
    {
        ProtectorDatosPersonales protector = new(ClavesPrueba.Anillo());

        string? primero = protector.Protect("Lucia Perez");
        string? segundo = protector.Protect("Lucia Perez");

        primero.Should().NotBe(segundo).And.NotContain("Lucia");
        protector.Unprotect(primero).Should().Be("Lucia Perez");
        protector.Unprotect(segundo).Should().Be("Lucia Perez");
    }

    [Fact]
    public void Un_valor_cifrado_alterado_no_se_descifra()
    {
        ProtectorDatosPersonales protector = new(ClavesPrueba.Anillo());
        byte[] cifrado = Convert.FromBase64String(protector.Protect("Lucia Perez")!);
        cifrado[^1] ^= 0x01;

        Action descifrar = () => protector.Unprotect(Convert.ToBase64String(cifrado));

        descifrar.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void Un_valor_nulo_entra_y_sale_nulo()
    {
        ProtectorDatosPersonales protector = new(ClavesPrueba.Anillo());

        protector.Protect(null!).Should().BeNull();
        protector.Unprotect(null!).Should().BeNull();
    }
}
