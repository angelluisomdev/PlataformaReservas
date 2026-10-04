using System.Security.Cryptography;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PlataformaReservas.Infraestructura;
using PlataformaReservas.Infraestructura.Identidad;
using PlataformaReservas.PruebasIntegracion.Infraestructura;

namespace PlataformaReservas.PruebasIntegracion.Identidad;

public sealed class PruebasProtectores
{
    private static AnilloClaves Anillo(string? claveDatos = null, string? claveBusqueda = null)
    {
        IConfiguration configuracion = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cifrado:ClaveDatos"] = claveDatos ?? BaseDatosFixture.ClaveAleatoria(),
                ["Cifrado:ClaveBusqueda"] = claveBusqueda ?? BaseDatosFixture.ClaveAleatoria(),
            })
            .Build();

        return new AnilloClaves(configuracion);
    }

    [Fact]
    public void El_valor_de_busqueda_es_determinista_y_distingue_entradas()
    {
        AnilloClaves anillo = Anillo();
        ProtectorBusqueda protector = new(anillo);

        string? primero = protector.Protect(anillo.CurrentKeyId, "ANA@EJEMPLO.COM");
        string? segundo = protector.Protect(anillo.CurrentKeyId, "ANA@EJEMPLO.COM");
        string? otro = protector.Protect(anillo.CurrentKeyId, "LUIS@EJEMPLO.COM");

        primero.Should().Be(segundo);
        primero.Should().NotBe(otro);
        primero.Should().NotContain("EJEMPLO");
    }

    [Fact]
    public void El_anillo_rechaza_claves_iguales()
    {
        string clave = BaseDatosFixture.ClaveAleatoria();

        Action crear = () => Anillo(clave, clave);

        crear.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Sin_claves_configuradas_la_comprobacion_de_arranque_falla()
    {
        IConfiguration configuracion = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PlataformaReservas"] = "Host=localhost",
            })
            .Build();
        ServiceCollection servicios = new();
        servicios.AddLogging();
        servicios.AgregarInfraestructura(configuracion);
        using ServiceProvider proveedor = servicios.BuildServiceProvider();

        Action comprobar = proveedor.ComprobarClavesCifrado;

        comprobar.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("no-es-base64")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAA==")]
    public void El_anillo_rechaza_claves_que_no_son_de_256_bits(string clave)
    {
        Action crear = () => Anillo(claveBusqueda: clave);

        crear.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void El_cifrado_va_y_vuelve_con_un_nonce_distinto_cada_vez()
    {
        ProtectorDatosPersonales protector = new(Anillo());

        string? primero = protector.Protect("Lucia Perez");
        string? segundo = protector.Protect("Lucia Perez");

        primero.Should().NotBe(segundo).And.NotContain("Lucia");
        protector.Unprotect(primero).Should().Be("Lucia Perez");
        protector.Unprotect(segundo).Should().Be("Lucia Perez");
    }

    [Fact]
    public void Un_valor_cifrado_alterado_no_se_descifra()
    {
        ProtectorDatosPersonales protector = new(Anillo());
        byte[] cifrado = Convert.FromBase64String(protector.Protect("Lucia Perez")!);
        cifrado[^1] ^= 0x01;

        Action descifrar = () => protector.Unprotect(Convert.ToBase64String(cifrado));

        descifrar.Should().Throw<CryptographicException>();
    }
}
