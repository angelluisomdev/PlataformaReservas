using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.Repositorios;

namespace PlataformaReservas.Aplicacion.CasosUso.Profesionales;

public sealed class AsociarServicio(
    IRepositorioProfesionales profesionales,
    IRepositorioServicios servicios,
    IUnidadTrabajo unidadTrabajo)
{
    public async Task<Resultado> EjecutarAsync(AsociarServicioComando comando, CancellationToken ct)
    {
        Profesional? profesional = await profesionales.ObtenerParaModificarAsync(comando.ProfesionalId, ct);
        if (profesional is null)
        {
            return Resultado.Fallo(CodigoError.NoEncontrado, "No se encuentra el profesional.");
        }

        Servicio? servicio = await servicios.ObtenerPorIdAsync(comando.ServicioId, ct);
        if (servicio is null)
        {
            return Resultado.Fallo(CodigoError.NoEncontrado, "No se encuentra el servicio.");
        }

        if (profesional.Presta(servicio.Id))
        {
            return Resultado.Fallo(CodigoError.Conflicto, "El profesional ya presta ese servicio.");
        }

        profesional.AsociarServicio(servicio.Id);
        await unidadTrabajo.GuardarCambiosAsync(ct);
        return Resultado.Exito();
    }
}
