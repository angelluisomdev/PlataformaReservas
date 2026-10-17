using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Dtos.Categorias;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Infraestructura.Persistencia;
using PlataformaReservas.PruebasIntegracion.Infraestructura;

namespace PlataformaReservas.PruebasIntegracion.Consultas;

[Collection("BaseDatos")]
public sealed class PruebasConsultasCatalogoPublico(BaseDatosFixture baseDatos)
{
    [Fact]
    public async Task Devuelve_las_categorias_activas_ordenadas_por_nombre_sin_contexto_de_empresa()
    {
        Categoria categoria = await EscenarioEmpresa.CrearCategoriaAsync(baseDatos);

        IReadOnlyList<CategoriaDto> categorias = await ObtenerCategoriasAsync();

        categorias.Should().Contain(new CategoriaDto(categoria.Id, categoria.Nombre.Valor, categoria.Slug.Valor));
        categorias.Select(c => c.Nombre).Should().BeInAscendingOrder(StringComparer.Ordinal);
    }

    [Fact]
    public async Task Una_categoria_inactiva_no_se_devuelve()
    {
        Categoria categoria = await EscenarioEmpresa.CrearCategoriaAsync(baseDatos);
        await DesactivarAsync(categoria.Id);

        IReadOnlyList<CategoriaDto> categorias = await ObtenerCategoriasAsync();

        categorias.Should().NotContain(c => c.Id == categoria.Id);
    }

    private async Task<IReadOnlyList<CategoriaDto>> ObtenerCategoriasAsync()
    {
        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        return await ambito.ServiceProvider.GetRequiredService<IConsultasCatalogoPublico>()
            .ObtenerCategoriasAsync(CancellationToken.None);
    }

    private async Task DesactivarAsync(Guid categoriaId)
    {
        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        await ambito.ServiceProvider.GetRequiredService<ContextoDatos>().Database
            .ExecuteSqlInterpolatedAsync($"UPDATE categorias SET activa = false WHERE id = {categoriaId}");
    }
}
