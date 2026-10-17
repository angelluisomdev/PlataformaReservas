using FluentAssertions;
using PlataformaReservas.Aplicacion.Dtos.Categorias;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.PruebasUnitarias.Dtos.Categorias;

public sealed class PruebasCategoriaDto
{
    [Fact]
    public void ConvertirADto_copia_el_identificador_el_nombre_y_el_slug_de_la_categoria()
    {
        Categoria categoria = Categoria.Crear(
            Guid.CreateVersion7(), new NombreCategoria("Entrenamiento personal"), Slug.Desde("Entrenamiento personal"));

        CategoriaDto dto = CategoriaDto.ConvertirADto(categoria);

        dto.Should().Be(new CategoriaDto(categoria.Id, "Entrenamiento personal", "entrenamiento-personal"));
    }
}
