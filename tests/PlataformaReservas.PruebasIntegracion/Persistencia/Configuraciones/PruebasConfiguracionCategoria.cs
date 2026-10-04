using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.ValueObjects;
using PlataformaReservas.Infraestructura.Persistencia;
using PlataformaReservas.PruebasIntegracion.Infraestructura;

namespace PlataformaReservas.PruebasIntegracion.Persistencia.Configuraciones;

[Collection("BaseDatos")]
public sealed class PruebasConfiguracionCategoria(BaseDatosFixture baseDatos)
{
    [Fact]
    public async Task Una_categoria_guardada_se_relee_con_su_nombre_y_su_slug()
    {
        Categoria categoria = DominioPrueba.Categoria(DominioPrueba.Sufijo());

        await using (AsyncServiceScope escritura = baseDatos.ServiciosIdentidad.CreateAsyncScope())
        {
            ContextoDatos db = escritura.ServiceProvider.GetRequiredService<ContextoDatos>();
            db.Add(categoria);
            await db.SaveChangesAsync();
        }

        await using AsyncServiceScope lectura = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        Categoria leida = await lectura.ServiceProvider.GetRequiredService<ContextoDatos>()
            .Categorias.SingleAsync(c => c.Id == categoria.Id);

        leida.Nombre.Should().Be(categoria.Nombre);
        leida.Slug.Should().Be(categoria.Slug);
        leida.Activa.Should().BeTrue();
    }

    [Fact]
    public async Task Dos_categorias_con_el_mismo_nombre_violan_el_indice_unico()
    {
        string sufijo = DominioPrueba.Sufijo();
        Categoria primera = DominioPrueba.Categoria(sufijo);
        Categoria segunda = Categoria.Crear(Guid.CreateVersion7(), primera.Nombre, Slug.Desde($"otra-{sufijo}"));

        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        ContextoDatos db = ambito.ServiceProvider.GetRequiredService<ContextoDatos>();
        db.AddRange(primera, segunda);

        Func<Task> guardar = () => db.SaveChangesAsync();

        await guardar.Should().ThrowAsync<DbUpdateException>();
    }
}
