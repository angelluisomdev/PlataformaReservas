using Microsoft.EntityFrameworkCore;
using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Dtos.Categorias;
using PlataformaReservas.Infraestructura.Persistencia;

namespace PlataformaReservas.Infraestructura.Consultas;

public sealed class ConsultasCatalogoPublico(ContextoDatos db) : IConsultasCatalogoPublico
{
    public async Task<IReadOnlyList<CategoriaDto>> ObtenerCategoriasAsync(CancellationToken ct)
    {
        return await db.Categorias
            .Where(c => c.Activa)
            .OrderBy(c => c.Nombre)
            .Select(c => CategoriaDto.ConvertirADto(c))
            .ToListAsync(ct);
    }
}
