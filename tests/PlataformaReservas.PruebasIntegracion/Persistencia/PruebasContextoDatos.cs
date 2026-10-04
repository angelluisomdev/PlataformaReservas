using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlataformaReservas.Infraestructura.Persistencia;
using PlataformaReservas.PruebasIntegracion.Infraestructura;

namespace PlataformaReservas.PruebasIntegracion.Persistencia;

[Collection("BaseDatos")]
public sealed class PruebasContextoDatos(BaseDatosFixture baseDatos)
{
    [Fact]
    public void Sin_protector_registrado_el_contexto_no_construye_el_modelo()
    {
        DbContextOptions<ContextoDatos> opciones = new DbContextOptionsBuilder<ContextoDatos>()
            .UseNpgsql(baseDatos.CadenaConexion)
            .UseSnakeCaseNamingConvention()
            .UseApplicationServiceProvider(new ServiceCollection().BuildServiceProvider())
            .EnableServiceProviderCaching(false)
            .Options;
        using ContextoDatos db = new(opciones);

        Action construir = () => _ = db.Model;

        construir.Should().Throw<InvalidOperationException>();
    }
}
