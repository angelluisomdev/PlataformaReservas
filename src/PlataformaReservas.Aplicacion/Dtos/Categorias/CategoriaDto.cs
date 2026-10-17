using PlataformaReservas.Dominio.Entidades;

namespace PlataformaReservas.Aplicacion.Dtos.Categorias;

public sealed record CategoriaDto(Guid Id, string Nombre, string Slug)
{
    public static CategoriaDto ConvertirADto(Categoria categoria)
    {
        return new CategoriaDto(categoria.Id, categoria.Nombre.Valor, categoria.Slug.Valor);
    }
}
