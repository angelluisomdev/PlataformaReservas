using Microsoft.Extensions.Configuration;
using PlataformaReservas.Infraestructura.Identidad;

namespace PlataformaReservas.PruebasIntegracion.Infraestructura;

public static class ClavesPrueba
{
    public static AnilloClaves Anillo(string? claveDatos = null, string? claveBusqueda = null)
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
}
