using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Dtos.Categorias;
using PlataformaReservas.Aplicacion.Dtos.Empresas;

namespace PlataformaReservas.PruebasUnitarias.CasosUso.Empresas;

public sealed class CatalogoPublicoFalso : IConsultasCatalogoPublico
{
    public List<(string Ciudad, string? Categoria, int Pagina, int TamanoPagina)> Busquedas { get; } = [];

    public Task<IReadOnlyList<CategoriaDto>> ObtenerCategoriasAsync(CancellationToken ct)
    {
        return Task.FromResult<IReadOnlyList<CategoriaDto>>([]);
    }

    public Task<PaginaResultados<ResumenEmpresaDto>> BuscarAsync(
        string ciudad, string? categoriaSlug, int pagina, int tamanoPagina, CancellationToken ct)
    {
        Busquedas.Add((ciudad, categoriaSlug, pagina, tamanoPagina));
        return Task.FromResult(new PaginaResultados<ResumenEmpresaDto>([], 0, pagina, tamanoPagina));
    }

    public Task<FichaEmpresaDto?> ObtenerFichaAsync(string slug, CancellationToken ct)
    {
        return Task.FromResult<FichaEmpresaDto?>(null);
    }
}
