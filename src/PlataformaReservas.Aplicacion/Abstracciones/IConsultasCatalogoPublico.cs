using PlataformaReservas.Aplicacion.Dtos.Categorias;
using PlataformaReservas.Aplicacion.Dtos.Empresas;

namespace PlataformaReservas.Aplicacion.Abstracciones;

public interface IConsultasCatalogoPublico
{
    Task<IReadOnlyList<CategoriaDto>> ObtenerCategoriasAsync(CancellationToken ct);

    Task<PaginaResultados<ResumenEmpresaDto>> BuscarAsync(
        string ciudad, string? categoriaSlug, int pagina, int tamanoPagina, CancellationToken ct);

    Task<FichaEmpresaDto?> ObtenerFichaAsync(string slug, CancellationToken ct);
}
