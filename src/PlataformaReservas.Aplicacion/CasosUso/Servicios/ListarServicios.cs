using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Aplicacion.Dtos;
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
            .Select(s => new ServicioDto(
                s.Id,
                s.Nombre.Valor,
                s.Descripcion?.Valor,
                s.Duracion.Minutos,
                s.Precio.Importe,
                s.IntervaloHuecosMinutos,
                s.AntelacionMinimaMinutos,
                s.AntelacionMaximaDias,
                s.Activo))
            .ToList();

        return Resultado<IReadOnlyList<ServicioDto>>.Exito(dtos);
    }
}
