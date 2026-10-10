using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Dtos;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.PruebasIntegracion.Infraestructura;

namespace PlataformaReservas.PruebasIntegracion.Consultas;

[Collection("BaseDatos")]
public sealed class PruebasConsultasCatalogoPublico(BaseDatosFixture baseDatos)
{
    [Fact]
    public async Task Devuelve_las_categorias_activas_ordenadas_por_nombre_sin_contexto_de_empresa()
    {
        Categoria categoria = await EscenarioEmpresa.CrearCategoriaAsync(baseDatos);

        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        IReadOnlyList<CategoriaDto> categorias = await ambito.ServiceProvider.GetRequiredService<IConsultasCatalogoPublico>()
            .ObtenerCategoriasAsync(CancellationToken.None);

        categorias.Should().Contain(new CategoriaDto(categoria.Id, categoria.Nombre.Valor));
        categorias.Select(c => c.Nombre).Should().BeInAscendingOrder(StringComparer.Ordinal);
    }
}
