using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Aplicacion.Dtos.Servicios;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.Repositorios;

namespace PlataformaReservas.Aplicacion.CasosUso.Servicios;

public sealed class ListarServicios(IRepositorioServicios servicios)
{
    public async Task<Resultado<IReadOnlyList<ServicioDto>>> EjecutarAsync(CancellationToken ct)
    {
        IReadOnlyList<Servicio> encontrados = await servicios.ListarAsync(ct);

        List<ServicioDto> dtos = encontrados
            .OrderBy(s => s.Nombre.Valor, StringComparer.CurrentCulture)
            .Select(ServicioDto.ConvertirADto)
            .ToList();

        return Resultado<IReadOnlyList<ServicioDto>>.Exito(dtos);
    }
}
