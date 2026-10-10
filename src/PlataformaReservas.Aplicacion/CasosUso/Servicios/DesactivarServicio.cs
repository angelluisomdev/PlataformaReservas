using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.Repositorios;

namespace PlataformaReservas.Aplicacion.CasosUso.Servicios;

public sealed class DesactivarServicio(IRepositorioServicios servicios, IUnidadTrabajo unidadTrabajo, IRelojSistema reloj)
{
    public async Task<Resultado> EjecutarAsync(DesactivarServicioComando comando, CancellationToken ct)
    {
        Servicio? servicio = await servicios.ObtenerParaModificarAsync(comando.Id, ct);
        if (servicio is null)
        {
            return Resultado.Fallo(CodigoError.NoEncontrado, "No se encuentra el servicio.");
        }

        if (!servicio.Activo)
        {
            return Resultado.Fallo(CodigoError.EstadoNoValido, "El servicio ya esta desactivado.");
        }

        servicio.Desactivar(reloj.AhoraUtc);
        await unidadTrabajo.GuardarCambiosAsync(ct);
        return Resultado.Exito();
    }
}
