using PlataformaReservas.Aplicacion.Dtos.Categorias;

namespace PlataformaReservas.Aplicacion.Abstracciones;

public interface IConsultasCatalogoPublico
{
    Task<IReadOnlyList<CategoriaDto>> ObtenerCategoriasAsync(CancellationToken ct);
}
