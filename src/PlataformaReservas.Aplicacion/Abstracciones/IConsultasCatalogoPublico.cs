using PlataformaReservas.Aplicacion.Dtos;

namespace PlataformaReservas.Aplicacion.Abstracciones;

public interface IConsultasCatalogoPublico
{
    Task<IReadOnlyList<CategoriaDto>> ObtenerCategoriasAsync(CancellationToken ct);
}
