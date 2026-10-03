using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PlataformaReservas.Dominio.ValueObjects;
using PlataformaReservas.Infraestructura.Persistencia;
using PlataformaReservas.Infraestructura.Persistencia.DatosIniciales;
using PlataformaReservas.PruebasIntegracion.Infraestructura;

namespace PlataformaReservas.PruebasIntegracion.Persistencia;

[Collection("BaseDatos")]
public sealed class PruebasDatosIniciales(BaseDatosFixture baseDatos)
{
    private static readonly string[] SlugsEsperados =
    [
        "peluqueria", "barberia", "estetica", "fisioterapia",
        "psicologia", "taller", "academia", "entrenamiento-personal",
    ];

    [Fact]
    public async Task Sembrar_dos_veces_deja_las_ocho_categorias_una_sola_vez()
    {
        await using (ContextoDatos db = baseDatos.CrearContexto())
        {
            await SembradorDatos.SembrarAsync(db, CancellationToken.None);
        }

        await using (ContextoDatos db = baseDatos.CrearContexto())
        {
            await SembradorDatos.SembrarAsync(db, CancellationToken.None);
        }

        await using ContextoDatos lectura = baseDatos.CrearContexto();
        List<Slug> sembradas = await lectura.Categorias.Select(c => c.Slug).ToListAsync();

        sembradas.Select(s => s.Valor)
            .Where(v => SlugsEsperados.Contains(v))
            .Should().BeEquivalentTo(SlugsEsperados);
    }
}
